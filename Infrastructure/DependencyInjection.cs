using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<BankDbContext>(options =>
                options.UseSqlite(connectionString, b => b.MigrationsAssembly("Infrastructure")));

            services.AddScoped<IBankDbContext>(provider => provider.GetRequiredService<BankDbContext>());

            return services;
        }
    }
}