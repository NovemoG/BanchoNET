using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Core.Models.Api.Account;

public sealed class UpdatePasswordRequest
{
    [FromForm(Name = "user[current_password]")]
    [Required(ErrorMessage = "is required")]
    public string CurrentPassword { get; set; } = null!;

    [FromForm(Name = "user[password]")]
    [Required(ErrorMessage = "is required")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "must be at least 8 characters")]
    public string Password { get; set; } = null!;

    [FromForm(Name = "user[password_confirmation]")]
    [Compare(nameof(Password), ErrorMessage = "does not match")]
    public string? PasswordConfirmation { get; set; }
}

public sealed class UpdateEmailRequest
{
    [FromForm(Name = "user[current_password]")]
    [Required(ErrorMessage = "is required")]
    public string CurrentPassword { get; set; } = null!;

    [FromForm(Name = "user[user_email]")]
    [Required(ErrorMessage = "is required")]
    [EmailAddress(ErrorMessage = "is not a valid email address")]
    [StringLength(160, ErrorMessage = "is too long")]
    public string Email { get; set; } = null!;

    [FromForm(Name = "user[user_email_confirmation]")]
    [Compare(nameof(Email), ErrorMessage = "does not match")]
    public string? EmailConfirmation { get; set; }
}