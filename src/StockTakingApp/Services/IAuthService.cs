using StockTakingApp.Models.Entities;

namespace StockTakingApp.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string email, string password);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
