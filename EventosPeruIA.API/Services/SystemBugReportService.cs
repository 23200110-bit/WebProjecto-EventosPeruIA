using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Services;
using EventosPeruIA.CORE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosPeruIA.API.Services;

public sealed class SystemBugReportService(PostgresContext db)
{
    public const int RequiredReporters = 3;

    public async Task<BugReportReceiptDTO> Submit(int userId, string description, CancellationToken ct)
    {
        var cleaned = Regex.Replace(description.Trim(), @"[\r\n\t]+", " ");
        cleaned = Regex.Replace(cleaned, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}", "[email]");
        cleaned = Regex.Replace(cleaned, @"(?<!\d)(?:\+?51)?9\d{8}(?!\d)", "[phone]");
        if (cleaned.Length is < 20 or > 1500)
            throw new ProyectoException(400, "La descripción debe tener entre 20 y 1500 caracteres.");

        var normalized = PromptInjectionGuard.Normalize(cleaned);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        var reportDay = DateOnly.FromDateTime(DateTime.UtcNow);
        var containsPromptInjection = PromptInjectionGuard.IsInjectionAttempt(cleaned);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.system_bug_reports
                (reporter_user_id, description, fingerprint, report_day, contains_prompt_injection)
            VALUES
                ({userId}, {cleaned}, {fingerprint}, {reportDay}, {containsPromptInjection})
            ON CONFLICT (reporter_user_id, fingerprint, report_day) DO NOTHING
            """, ct);

        var reportId = await db.Database.SqlQuery<long>($"""
            SELECT id AS "Value"
            FROM public.system_bug_reports
            WHERE reporter_user_id = {userId}
              AND fingerprint = {fingerprint}
              AND report_day = {reportDay}
            """).SingleAsync(ct);
        var distinctReporters = await db.Database.SqlQuery<int>($"""
            SELECT count(DISTINCT reporter_user_id)::int AS "Value"
            FROM public.system_bug_reports
            WHERE fingerprint = {fingerprint}
              AND created_at >= now() - interval '24 hours'
            """).SingleAsync(ct);
        var queued = distinctReporters >= RequiredReporters && await db.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1
                FROM public.system_bug_incidents
                WHERE fingerprint = {fingerprint}
                  AND status IN ('pending', 'processing', 'awaiting_restart')
            ) AS "Value"
            """).SingleAsync(ct);

        return new(reportId, distinctReporters, queued);
    }
}
