using Domain.Entities;

namespace Infrastructure.Authentication
{
    public interface IJwtTokenGenerator
    {
        string GenerateRefreshToken();
        string GenerateToken(User user);
    }
}