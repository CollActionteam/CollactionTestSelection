using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;

namespace CollactionTestSelection.Auth
{
    public class BasicAuthenticationOptions : AuthenticationSchemeOptions
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}