using System.Security.Claims;
using BooksPlusWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BooksPlusWeb.Controllers;

// Signing in and out, ready to use. It needs UserService registered for dependency
// injection, and cookie authentication turned on, in Program.cs.
public class AccountController : Controller
{
    private readonly UserService userService;

    public AccountController(UserService userService)
    {
        this.userService = userService;
    }

    // GET /Account/Login?returnUrl=/books/new
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl)
    {
        AppUser? user = userService.SignIn(userName, password);
        if (user is null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Error"] = "That user name and password don't match.";
            return View();
        }

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    // POST /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Denied
    public IActionResult Denied()
    {
        return View();
    }
}
