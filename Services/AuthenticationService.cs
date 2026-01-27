using Inkwell_Kunal.Data;
using Microsoft.EntityFrameworkCore;

namespace Inkwell_Kunal.Services;

public class AuthenticationService
{
    private readonly AppDbContext _db;
    private User? _currentUser;

    public event Action? OnChange;

    public AuthenticationService(AppDbContext db)
    {
        _db = db;
        _db.Database.EnsureCreated();
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser != null;

    public async Task<bool> RegisterAsync(string username, string password, string? pin = null)
    {
        if (await _db.Users.AnyAsync(u => u.Username == username))
            return false; // Username exists

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            PinHash = pin != null ? HashPassword(pin) : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
            return false;

        _currentUser = user;
        OnChange?.Invoke();
        return true;
    }

    public async Task<bool> LoginWithPinAsync(string username, string pin)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || user.PinHash == null || !VerifyPassword(pin, user.PinHash))
            return false;

        _currentUser = user;
        OnChange?.Invoke();
        return true;
    }

    public void Logout()
    {
        _currentUser = null;
        OnChange?.Invoke();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _db.Users.ToListAsync();
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (CurrentUser == null) return false;

        // Verify current password
        if (!VerifyPassword(currentPassword, CurrentUser.PasswordHash))
        {
            return false;
        }

        // Hash new password
        var newHash = HashPassword(newPassword);

        // Update in database
        var user = await _db.Users.FindAsync(CurrentUser.Id);
        if (user == null) return false;

        user.PasswordHash = newHash;
        await _db.SaveChangesAsync();

        // Update current user
        CurrentUser.PasswordHash = newHash;

        return true;
    }
}