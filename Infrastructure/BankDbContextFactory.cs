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
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Rentaldb;Username=postgres;Password=1976;Pooling=true;");

            return new BankDbContext(optionsBuilder.Options);
        }
    }
}
