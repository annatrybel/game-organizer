namespace GameOrganizer.Api.Models.Dto.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public LoginResponse(string token)
        {
            Token = token;
        }
    }
}
