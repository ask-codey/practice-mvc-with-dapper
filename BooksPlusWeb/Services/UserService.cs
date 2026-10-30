using Microsoft.AspNetCore.Identity;

namespace BooksPlusWeb.Services;

public class AppUser
{
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
}

// Checks user names and passwords, ready to use. To keep the exercise short, the users live
// in memory; a real app keeps them, with their password hashes, in a database table.
//
//   admin / Bookworm-Admin-2026   (role Admin: may add books)
//   clerk / Bookworm-Clerk-2026   (role Staff)
public class UserService
{
    private readonly PasswordHasher<AppUser> hasher = new PasswordHasher<AppUser>();
    private readonly List<AppUser> users = new List<AppUser>();

    public UserService()
    {
        AddUser("admin", "Bookworm-Admin-2026", "Admin");
        AddUser("clerk", "Bookworm-Clerk-2026", "Staff");
    }

    // The user, if the name and password match; otherwise null.
    public AppUser? SignIn(string userName, string password)
    {
        AppUser? user = users.Find(u => u.UserName == userName);
        if (user is null)
        {
            return null;
        }

        PasswordVerificationResult result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    private void AddUser(string userName, string password, string role)
    {
        AppUser user = new AppUser { UserName = userName, Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);
        users.Add(user);
    }
}
