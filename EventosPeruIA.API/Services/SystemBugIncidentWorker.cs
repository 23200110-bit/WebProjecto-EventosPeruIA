using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.API.Services;

public sealed class SystemBugIncidentWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<SystemBugIncidentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (string.Equals(config["AdminAgent:LocalSupervisorEnabled"], "true", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<SystemBugIncidentProcessor>().ProcessOne(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Automatic processing of repeated user reports failed.");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}

public sealed class SystemBugIncidentProcessor(PostgresContext db, AdminAgentService agent, ILogger<SystemBugIncidentProcessor> logger)
{
    public async Task ProcessOne(CancellationToken ct)
    {
        var incident = await db.Database.SqlQuery<PendingBugIncidentDTO>($"""
            SELECT fingerprint AS "Fingerprint",
                   summary AS "Summary",
                   report_count AS "ReportCount",
                   distinct_reporters AS "DistinctReporters",
                   attempt_count AS "AttemptCount"
            FROM public.system_bug_incidents
            WHERE status = 'pending'
              AND distinct_reporters >= {SystemBugReportService.RequiredReporters}
              AND (
                  SELECT count(DISTINCT reports.reporter_user_id)
                  FROM public.system_bug_reports reports
                  WHERE reports.fingerprint = system_bug_incidents.fingerprint
                    AND reports.created_at >= now() - interval '24 hours'
              ) >= {SystemBugReportService.RequiredReporters}
              AND attempt_count < 3
              AND (last_attempt_at IS NULL OR last_attempt_at <= now() - interval '30 minutes')
            ORDER BY first_reported_at
            LIMIT 1
            """).FirstOrDefaultAsync(ct);
        if (incident == null) return;

        var claimed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE public.system_bug_incidents
            SET status = 'processing',
                attempt_count = attempt_count + 1,
                last_attempt_at = now()
            WHERE fingerprint = {incident.Fingerprint}
              AND status = 'pending'
              AND (last_attempt_at IS NULL OR last_attempt_at <= now() - interval '30 minutes')
            """, ct);
        if (claimed != 1) return;

        try
        {
            var reports = await db.Database.SqlQuery<BugReportEvidenceDTO>($"""
                SELECT description AS "Description",
                       contains_prompt_injection AS "ContainsPromptInjection"
                FROM public.system_bug_reports
                WHERE fingerprint = {incident.Fingerprint}
                  AND created_at >= now() - interval '24 hours'
                ORDER BY created_at
                LIMIT 10
                """).ToListAsync(ct);
            var evidence = reports.Select(report => new BugReportEvidenceDTO
            {
                Description = report.ContainsPromptInjection
                    ? "[Contenido omitido: se detectaron instrucciones no confiables en este reporte.]"
                    : report.Description,
                ContainsPromptInjection = report.ContainsPromptInjection
            }).ToArray();
            var result = await agent.RunAutomaticIncident(incident, evidence, ct);
            if (result.RestartManifest is { } manifest && result.ProposedPatch is { } patch)
            {
                var linked = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE public.system_bug_incidents
                    SET status = 'awaiting_restart',
                        admin_code_patch_id = {patch.Id},
                        validation_output = {result.Reply}
                    WHERE fingerprint = {incident.Fingerprint}
                      AND status = 'processing'
                    """, ct);
                if (linked != 1)
                    logger.LogWarning("Could not link admin patch {PatchId} to incident {Fingerprint}; signaling the validated restart anyway.", patch.Id, incident.Fingerprint);
                await agent.SignalRestartAsync(manifest);
                return;
            }

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE public.system_bug_incidents
                SET status = 'no_patch', validation_output = {result.Reply}
                WHERE fingerprint = {incident.Fingerprint}
                  AND status = 'processing'
                """, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Automatic diagnosis failed for incident {Fingerprint}.", incident.Fingerprint);
            var failedStatus = incident.AttemptCount + 1 >= 3 ? "rejected" : "pending";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE public.system_bug_incidents
                SET status = {failedStatus}, validation_output = {exception.Message}
                WHERE fingerprint = {incident.Fingerprint}
                  AND status = 'processing'
                """, CancellationToken.None);
        }
    }
}
