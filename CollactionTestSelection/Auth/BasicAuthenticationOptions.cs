using Microsoft.AspNetCore.Authentication;

namespace CollactionTestSelection.Auth
{
    public class BasicAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Username { get; set; }
        
        public string Password { get; set; }
    }
}