using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Authentication;
using Infrastructure;
using Domain.Entities;
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
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Infrastructure")));

            services.AddScoped<IBankDbContext>(provider => provider.GetRequiredService<BankDbContext>());
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            return services;
        }
    }
}