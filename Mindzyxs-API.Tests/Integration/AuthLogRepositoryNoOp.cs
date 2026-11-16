using Mindzyxs_API.Repositories;

namespace Mindzyxs_API.Tests.Integration;

public class AuthLogRepositoryNoOp : IAuthLogRepository
{
    public Task LogAsync(int? actorId, int? targetId, string accion, string detalle, string? ip, CancellationToken ct = default)
        => Task.CompletedTask;
}
