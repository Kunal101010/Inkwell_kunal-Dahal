using Inkwell_Kunal.Data;
using System.Security.Cryptography;
using System.Text;
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
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
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
}