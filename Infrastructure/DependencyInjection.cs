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
                options.UseSqlite(connectionString, b => b.MigrationsAssembly("Infrastructure")));

            services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<BankDbContext>();

            services.AddScoped<IBankDbContext>(provider => provider.GetRequiredService<BankDbContext>());

            services.AddScoped<IBankDbContext, BankDbContext>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            return services;
        }
    }
}