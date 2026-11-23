using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventosPeruIA.API.Services;

public sealed class AdminAgentService(
    HttpClient http,
    PostgresContext db,
    IConfiguration config,
    ILogger<AdminAgentService> logger)
{
    private const int MaxToolRounds = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdminAgentResponse> Run(string prompt, CancellationToken ct)
    {
        var apiKey = config["AI:Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ProyectoException(503, "Configura AI:Gemini:ApiKey en secretos del servidor.");

        var messages = new List<object> { new { role = "user", parts = new[] { new { text = prompt } } } };
        var executed = new List<string>();
        AdminProposedPatch? latestPatch = null;
        var model = config["AI:Gemini:AdminModel"] ?? "gemini-4-neon";

        for (var round = 0; round < MaxToolRounds; round++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = "Eres un agente de diagnóstico de EventosPeruIA. Usa herramientas para consultar datos concretos. " +
                                     "Solo puedes reindexar embeddings, nunca ejecutar SQL arbitrario ni modificar producción. " +
                                     "Los cambios de código se guardan como propuestas y, si está configurado, se envían como un pull request draft para revisión humana. " +
                                     "No afirmes que una acción tuvo éxito si la herramienta no lo confirmó." }
                    }
                },
                contents = messages,
                tools = new[] { new { functionDeclarations = ToolDeclarations } },
                generationConfig = new { thinkingConfig = new { thinkingLevel = "high" } }
            });

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini admin agent returned HTTP {StatusCode}.", (int)response.StatusCode);
                throw new ProyectoException(response.StatusCode == HttpStatusCode.TooManyRequests ? 429 : 503,
                    "El agente de Gemini no está disponible.");
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
                throw new ProyectoException(502, "El agente de Gemini devolvió una respuesta vacía.");
            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                throw new ProyectoException(502, "El agente de Gemini devolvió una respuesta inválida.");
            var calls = parts.EnumerateArray()
                .Where(part => part.TryGetProperty("functionCall", out _))
                .Select(part => part.GetProperty("functionCall").Clone()).ToArray();
            if (calls.Length == 0)
            {
                var reply = string.Concat(parts.EnumerateArray()
                    .Where(part => !part.TryGetProperty("thought", out var thought) || !thought.GetBoolean())
                    .Select(part => part.TryGetProperty("text", out var text) ? text.GetString() : ""));
                if (string.IsNullOrWhiteSpace(reply))
                    throw new ProyectoException(502, "El agente no devolvió una respuesta válida.");
                return new(reply, executed, latestPatch);
            }

            messages.Add(content.Clone());
            var toolResponses = new List<object>();
            foreach (var call in calls)
            {
                var name = call.GetProperty("name").GetString() ?? "";
                var args = call.TryGetProperty("args", out var arguments) ? arguments : default;
                var result = await ExecuteTool(name, args, ct);
                executed.Add(name);
                if (name == "propose_code_fix")
                    latestPatch = JsonSerializer.Deserialize<AdminProposedPatch>(result.GetProperty("patch").GetRawText(), JsonOptions);
                toolResponses.Add(new
                {
                    functionResponse = new { name, response = new { result } }
                });
            }
            messages.Add(new { role = "user", parts = toolResponses });
        }

        throw new ProyectoException(502, "El agente superó el límite de llamadas a herramientas.");
    }

    private async Task<JsonElement> ExecuteTool(string name, JsonElement args, CancellationToken ct)
    {
        if (args.ValueKind != JsonValueKind.Object)
            throw new ProyectoException(502, "El agente devolvió argumentos de herramienta inválidos.");
        switch (name)
        {
            case "query_system_diagnostics":
            {
                var pending = await db.Database.SqlQuery<int>($"""
                    SELECT count(*)::int AS "Value" FROM public.document_sections WHERE embedding IS NULL
                    """).SingleAsync(ct);
                var patches = await db.Database.SqlQuery<int>($"""
                    SELECT count(*)::int AS "Value" FROM public.admin_code_patches WHERE status = 'pending'
                    """).SingleAsync(ct);
                return JsonSerializer.SerializeToElement(new
                {
                    unvectorizedSections = pending,
                    pendingCodePatches = patches,
                    note = "Los logs de aplicación no se almacenan en una tabla consultable por este agente."
                });
            }
            case "execute_admin_db_action":
            {
                var action = RequiredString(args, "action");
                if (action != "reindex_embeddings")
                    throw new ProyectoException(400, "La acción administrativa no está permitida.");
                var changed = await db.Database.ExecuteSqlRawAsync(
                    "UPDATE public.document_sections SET embedding = NULL WHERE embedding IS NOT NULL", ct);
                return JsonSerializer.SerializeToElement(new { action, sectionsQueued = changed });
            }
            case "propose_code_fix":
                return await ProposePatch(args, ct);
            default:
                throw new ProyectoException(400, "Herramienta administrativa desconocida.");
        }
    }

    private async Task<JsonElement> ProposePatch(JsonElement args, CancellationToken ct)
    {
        var summary = RequiredString(args, "error_summary");
        var target = RequiredString(args, "target_repo");
        var path = RequiredString(args, "file_path");
        var correctedContent = RequiredString(args, "corrected_content");
        var diff = RequiredString(args, "diff_content");
        if (target is not ("frontend" or "backend") || !ValidRelativePath(path) ||
            path.Length > 512 || summary.Length > 1000 || correctedContent.Length > 500_000 || diff.Length > 500_000)
            throw new ProyectoException(400, "La propuesta de código contiene valores inválidos.");

        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        long id;
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO public.admin_code_patches
                    (error_summary, target_repo, file_path, diff_content)
                VALUES (@summary, @target, @path, @diff)
                RETURNING id
                """;
            command.Parameters.Add(new NpgsqlParameter("summary", summary));
            command.Parameters.Add(new NpgsqlParameter("target", target));
            command.Parameters.Add(new NpgsqlParameter("path", path));
            command.Parameters.Add(new NpgsqlParameter("diff", diff));
            id = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        string? prUrl = null;
        string? prError = null;
        var token = config["GITHUB_TOKEN"];
        var repository = config[$"AdminAgent:GitHub:{(target == "backend" ? "BackendRepository" : "FrontendRepository")}"]
            ?? (target == "backend" ? "23200110-bit/WebProjecto-EventosPeruIA" : null);
        if (!string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(repository))
            prError = $"Configura AdminAgent:GitHub:{(target == "backend" ? "BackendRepository" : "FrontendRepository")} para crear el PR.";
        else if (string.IsNullOrWhiteSpace(token))
            prError = "GITHUB_TOKEN no está configurado; la propuesta se guardó sin crear un PR.";
        else if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                prUrl = await CreateDraftPullRequest(token, repository, id, summary, path, correctedContent, ct);
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE public.admin_code_patches SET pr_url = {prUrl} WHERE id = {id}", ct);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
            {
                logger.LogError(exception, "Could not create draft PR for admin patch {PatchId}.", id);
                prError = "La propuesta se guardó, pero GitHub no pudo crear el PR draft. Revisa los logs del servidor y los permisos del token.";
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogError("GitHub timed out while creating draft PR for admin patch {PatchId}.", id);
                prError = "La propuesta se guardó, pero GitHub agotó el tiempo de espera al crear el PR draft.";
            }
        }

        var patch = new AdminProposedPatch(id, target, path, diff, prUrl, "pending");
        return JsonSerializer.SerializeToElement(new { patch, prError });
    }

    private async Task<string> CreateDraftPullRequest(string token, string repository, long id, string summary,
        string path, string correctedContent, CancellationToken ct)
    {
        var parts = repository.Split('/');
        if (parts.Length != 2 || parts.Any(part => string.IsNullOrWhiteSpace(part) ||
            part.Any(character => !char.IsLetterOrDigit(character) && character is not ('.' or '_' or '-'))))
            throw new InvalidOperationException("Configure the GitHub target as owner/repository.");

        using var client = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EventosPeruIA-AdminAgent/1.0");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        var repoPath = $"repos/{parts[0]}/{parts[1]}";
        using var repoResponse = await client.GetAsync(repoPath, ct);
        repoResponse.EnsureSuccessStatusCode();
        using var repoJson = await JsonDocument.ParseAsync(await repoResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var baseBranch = repoJson.RootElement.GetProperty("default_branch").GetString()
            ?? throw new JsonException("GitHub repository has no default branch.");
        using var refResponse = await client.GetAsync($"{repoPath}/git/ref/heads/{Uri.EscapeDataString(baseBranch)}", ct);
        refResponse.EnsureSuccessStatusCode();
        using var refJson = await JsonDocument.ParseAsync(await refResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var baseSha = refJson.RootElement.GetProperty("object").GetProperty("sha").GetString()
            ?? throw new JsonException("GitHub base branch has no SHA.");
        var branch = $"admin-agent/patch-{id}";
        using var createRef = await client.PostAsJsonAsync($"{repoPath}/git/refs",
            new { @ref = $"refs/heads/{branch}", sha = baseSha }, ct);
        createRef.EnsureSuccessStatusCode();

        using var fileResponse = await client.GetAsync($"{repoPath}/contents/{EscapePath(path)}?ref={Uri.EscapeDataString(baseBranch)}", ct);
        fileResponse.EnsureSuccessStatusCode();
        using var fileJson = await JsonDocument.ParseAsync(await fileResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var fileSha = fileJson.RootElement.GetProperty("sha").GetString()
            ?? throw new JsonException("GitHub target file has no SHA.");
        using var updateResponse = await client.PutAsJsonAsync($"{repoPath}/contents/{EscapePath(path)}", new
        {
            message = $"Propose admin agent fix #{id}",
            content = Convert.ToBase64String(Encoding.UTF8.GetBytes(correctedContent)),
            sha = fileSha,
            branch
        }, ct);
        updateResponse.EnsureSuccessStatusCode();
        using var prResponse = await client.PostAsJsonAsync($"{repoPath}/pulls", new
        {
            title = $"[Admin Agent] {summary}",
            head = branch,
            @base = baseBranch,
            body = $"Automated proposal #{id}. Review and test before merging.",
            draft = true
        }, ct);
        prResponse.EnsureSuccessStatusCode();
        using var prJson = await JsonDocument.ParseAsync(await prResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return prJson.RootElement.GetProperty("html_url").GetString()
            ?? throw new JsonException("GitHub pull request URL missing.");
    }

    private static string EscapePath(string path) => string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

    private static bool ValidRelativePath(string path) =>
        !path.StartsWith('/') && !path.Contains('\\') &&
        !path.Any(char.IsControl) &&
        path.Split('/').All(part => part.Length > 0 && part is not ("." or ".."));

    private static string RequiredString(JsonElement args, string name)
    {
        if (!args.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new ProyectoException(400, $"Falta el parámetro requerido {name}.");
        return value.GetString()!.Trim();
    }

    private static readonly object[] ToolDeclarations =
    [
        new
        {
            name = "query_system_diagnostics",
            description = "Consulta la cantidad de documentos sin embeddings y propuestas pendientes.",
            parameters = new { type = "OBJECT", properties = new { }, required = Array.Empty<string>() }
        },
        new
        {
            name = "execute_admin_db_action",
            description = "Encola la reindexación de todos los embeddings RAG.",
            parameters = new
            {
                type = "OBJECT",
                properties = new { action = new { type = "STRING", @enum = new[] { "reindex_embeddings" } } },
                required = new[] { "action" }
            }
        },
        new
        {
            name = "propose_code_fix",
            description = "Guarda un diff y el contenido corregido, y opcionalmente abre un PR draft.",
            parameters = new
            {
                type = "OBJECT",
                properties = new
                {
                    error_summary = new { type = "STRING" },
                    target_repo = new { type = "STRING", @enum = new[] { "frontend", "backend" } },
                    file_path = new { type = "STRING" },
                    diff_content = new { type = "STRING" },
                    corrected_content = new { type = "STRING" }
                },
                required = new[] { "error_summary", "target_repo", "file_path", "diff_content", "corrected_content" }
            }
        }
    ];
}

public sealed record AdminAgentResponse(string Reply, IReadOnlyList<string> ToolsExecuted, AdminProposedPatch? ProposedPatch);
public sealed record AdminProposedPatch(
    long Id,
    [property: JsonPropertyName("target_repo")] string TargetRepo,
    [property: JsonPropertyName("file_path")] string FilePath,
    [property: JsonPropertyName("diff_content")] string DiffContent,
    [property: JsonPropertyName("pr_url")] string? PrUrl,
    string Status);
