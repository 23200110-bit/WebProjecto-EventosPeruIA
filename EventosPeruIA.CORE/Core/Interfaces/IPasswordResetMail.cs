namespace EventosPeruIA.CORE.Core.Interfaces;
public interface IPasswordResetMail
{
    void ValidateConfiguration();
    Task Send(string recipient, string resetUrl, CancellationToken ct);
}
