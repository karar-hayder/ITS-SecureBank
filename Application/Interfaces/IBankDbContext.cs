using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces
{
    public interface IBankDbContext
    {
        DbSet<Account> Accounts { get; set; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<Transaction> Transactions { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<AuditLog> AuditLogs { get; set; }
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}