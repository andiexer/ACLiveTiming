using System.Security.Claims;
using Devlabs.AcTiming.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Devlabs.AcTiming.Web.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly PasswordService _passwords;

    public bool Error { get; private set; }

    public LoginModel(PasswordService passwords) => _passwords = passwords;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect("/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? password, string? returnUrl)
    {
        if (!_passwords.Verify(password ?? ""))
        {
            Error = true;
            return Page();
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "admin") };
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true }
        );

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}
