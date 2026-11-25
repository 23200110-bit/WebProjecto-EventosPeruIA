using EventosPeruIA.CORE.Core.DTOs;

namespace EventosPeruIA.CORE.Core.Interfaces;

public interface IFlyerExtractor
{
    Task<FlyerDraftDTO> Extract(FlyerDraftRequestDTO request, CancellationToken ct);
}
