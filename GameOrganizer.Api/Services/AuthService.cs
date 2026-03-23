using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services.Errors;
using GameOrganizer.Api.Services.Interfaces;
using GameOrganizer.Api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace GameOrganizer.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICollectionService _collectionService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileService _fileService;
        private const string ObjectName = "User";

        public AuthService(UserManager<ApplicationUser> userManager,
        ICollectionService collectionService,
        ILogger<AuthService> logger,
        IConfiguration configuration,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment hostingEnvironment, 
        IEmailSender emailSender,
        IHttpContextAccessor httpContextAccessor,
        IFileService fileService)
        {
            _userManager = userManager;
            _collectionService = collectionService;
            _logger = logger;
            _configuration = configuration;
            _signInManager = signInManager;
            _hostingEnvironment = hostingEnvironment;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
            _fileService = fileService;
        }

        public async Task<ServiceResult> RegisterAsync(RegisterDto registerDto, bool isAdmin = false)
        {
            if (string.IsNullOrWhiteSpace(registerDto.Email))
            {
                return ServiceResult.Failure(AuthErrors.EmailRequired());
            }

            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return ServiceResult.Failure(AuthErrors.UserAlreadyExists());
            }

            var userByUsername = await _userManager.FindByNameAsync(registerDto.Username);
            if (userByUsername != null)
            {
                return ServiceResult.Failure(AuthErrors.UsernameTaken());
            }

            var user = new ApplicationUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Administrator");

                if (admins.Count == 0)
                {
                    await _userManager.AddToRoleAsync(user, "Administrator");
                    _logger.LogInformation("Pierwszy użytkownik utworzony jako Administrator.");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("Użytkownik utworzony z rolą User.");
                }
                await _collectionService.InitDefaultCollectionsAsync(user.Id);

                _logger.LogInformation("User created a new account with password and default collections.");
            }
            else
            {
                var details = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(AuthErrors.UserCreationFailed(details));
            }
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> CreateAdminUser(RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return ServiceResult.Failure(AuthErrors.UserAlreadyExists());
            }

            var user = new ApplicationUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Administrator");
                return ServiceResult.Success();
            }

            return ServiceResult.Failure(AuthErrors.UserCreationFailed());
        }


        public async Task<ServiceResult<LoginResponse>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null) return ServiceResult<LoginResponse>.Failure(AuthErrors.InvalidCredentials());

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return ServiceResult<LoginResponse>.Failure("Konto jest zablokowane. Spróbuj za 15 minut.");
            }

            if (!result.Succeeded)
            {
                return ServiceResult<LoginResponse>.Failure(AuthErrors.InvalidCredentials());
            }

            var token = await GenerateJwtTokenAsync(user);

            return ServiceResult<LoginResponse>.Success(new LoginResponse(token));
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var secret = _configuration["JWT:Secret"];
            var issuer = _configuration["JWT:Issuer"];
            var audience = _configuration["JWT:Audience"];

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                throw new InvalidOperationException("Konfiguracja JWT jest niekompletna.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(7);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

            if (user == null)
            {
                return ServiceResult.Success();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var frontEndBaseUrl = _configuration["FRONTEND_BASE_URL"];
            if (string.IsNullOrEmpty(frontEndBaseUrl))
            {
                Console.WriteLine("OSTRZEŻENIE: Brak konfiguracji FRONTEND_BASE_URL. Używam domyślnego adresu 'http://localhost:3000' do wygenerowania linku resetowania hasła.");
                frontEndBaseUrl = "http://localhost:3000";
            }

            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(user.Email);

            var callbackUrl = $"{frontEndBaseUrl}/reset-password?token={encodedToken}&email={encodedEmail}";

            var templatePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Templates", "ForgotPasswordEmail.html");
            var emailBody = await File.ReadAllTextAsync(templatePath);

            emailBody = emailBody.Replace("{UserName}", user.UserName ?? "użytkowniku");
            emailBody = emailBody.Replace("{CallbackUrl}", callbackUrl);

            var subject = "Reset hasła";
            await _emailSender.SendEmailAsync(user.Email, subject, emailBody);
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return ServiceResult.Failure(CommonErrors.BadRequest("Nieudana próba resetu hasła."));
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (result.Succeeded)
            {
                return ServiceResult.Success();
            }

            var errors = result.Errors.Select(e => e.Description);
            string errorMessage = string.Join(", ", errors);
            return ServiceResult.Failure(errorMessage);
        }

        public async Task<ServiceResult<LoginResponse>> HandleExternalLoginAsync()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("Brak informacji od zewnętrznego dostawcy.");
                return ServiceResult<LoginResponse>.Failure(AuthErrors.ExternalAuthFailed("Unknown"));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("Użytkownik zalogowany przez {Name} provider.", info.LoginProvider);
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

                var token = await GenerateJwtTokenAsync(user);
                return ServiceResult<LoginResponse>.Success(new LoginResponse(token));
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    return ServiceResult<LoginResponse>.Failure(AuthErrors.EmailRequired()); 
                }

                var user = await _userManager.FindByEmailAsync(email);
                var isNewUser = user == null;

                if (isNewUser)
                {
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var createUserResult = await _userManager.CreateAsync(user);
                    if (!createUserResult.Succeeded)
                    {
                        var err = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                        _logger.LogError("Błąd tworzenia usera OAuth: {Err}", err);
                        return ServiceResult<LoginResponse>.Failure(AuthErrors.UserCreationFailed());
                    }
                    _logger.LogInformation("Utworzono nowego użytkownika z emailem {Email} przez logowanie zewnętrzne.", email);

                    var admins = await _userManager.GetUsersInRoleAsync("Administrator");
                    if (admins.Count == 0)
                    {
                        await _userManager.AddToRoleAsync(user, "Administrator");
                        _logger.LogInformation("Pierwszy użytkownik (zewnętrzny) został administratorem.");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        _logger.LogInformation("Nowy użytkownik (zewnętrzny) otrzymał rolę User.");
                    }
                    await _collectionService.InitDefaultCollectionsAsync(user.Id);
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    return ServiceResult<LoginResponse>.Failure(AuthErrors.AccountLinkingFailed());
                }

                _logger.LogInformation("Konto dla {Email} zostało powiązane z dostawcą {LoginProvider}", user.Email, info.LoginProvider);

                var finalToken = await GenerateJwtTokenAsync(user);
                return ServiceResult<LoginResponse>.Success(new LoginResponse(finalToken));
            }
        }

        public async Task<ServiceResult<UserDto>> GetMe()
        {
            var userPrincipal = _httpContextAccessor.HttpContext?.User;
            var username = userPrincipal?.Identity?.Name;           
            if (string.IsNullOrEmpty(username))
            {
                var userId = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                return ServiceResult<UserDto>.Failure(CommonErrors.NotFound(ObjectName, userId ?? "Unknown"));
            }

            var user = new UserDto
            {
                UserName = username
            };
            return ServiceResult<UserDto>.Success(user);
        }

        public async Task<ServiceResult> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.Failure(CommonErrors.NotFound("User", userId));

            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.UserName)
            {
                var existing = await _userManager.FindByNameAsync(dto.Username);
                if (existing != null)
                    return ServiceResult.Failure(new ServiceError("User.UsernameTaken", "Ta nazwa użytkownika jest już zajęta."));

                user.UserName = dto.Username;
            }

            user.Bio = dto.Bio;

            if (dto.Avatar != null)
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.Contains("cloudinary.com"))
                {
                    await _fileService.DeleteImageAsync(user.AvatarUrl);
                }
                user.AvatarUrl = await _fileService.UploadImageAsync(dto.Avatar);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ServiceResult.Failure("Wystąpił błąd podczas zapisywania profilu.");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return ServiceResult.Success();
        }
    }
}



