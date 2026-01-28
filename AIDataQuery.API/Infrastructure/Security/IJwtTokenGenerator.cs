namespace AIDataQuery.API.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(int userId, string username, string role, int expireHours = 8);
}
