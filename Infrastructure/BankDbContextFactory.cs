using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    public class BankDbContextFactory : IDesignTimeDbContextFactory<BankDbContext>
    {
        public BankDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BankDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bank_db;Username=postgres;Password=password");

            return new BankDbContext(optionsBuilder.Options);
        }
    }
}
