using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Results;

namespace GameOrganizer.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult> RegisterAsync(RegisterDto registerDto, bool isAdmin = false);
        Task<ServiceResult<LoginResponse>> Login(LoginDto loginDto);
        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<ServiceResult<LoginResponse>> HandleExternalLoginAsync();
        Task<ServiceResult<UserDto>> GetMe();
    }
}