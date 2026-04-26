using CrewService.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CrewService.Infrastructure.Outbox;

/// <summary>
/// Abstraction for database contexts that support outbox message persistence.
/// </summary>
public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
