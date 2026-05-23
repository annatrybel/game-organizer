using GameOrganizer.Api.Hubs;
using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Seeders;
using GameOrganizer.Api.Services;
using GameOrganizer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

// ==========================================
// ŁADOWANIE KONFIGURACJI (.env)
// ==========================================

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);


// ==========================================
// BAZA DANYCH I IDENTITY
// ==========================================

string rawConnectionString;
if (builder.Configuration.GetValue<bool>("IS_IN_CONTAINER"))
{
    // Uruchomienie w kontenerze Docker (lub na produkcji)
    rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
else
{
    // Uruchomienie lokalne z Visual Studio
    rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection_LOCAL");
}

var connectionString = ConnectionStringConverter.Convert(rawConnectionString);

builder.Services.AddDbContext<GameOrganizerDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<GameOrganizerDbContext>()
    .AddDefaultTokenProviders();

// ==========================================
// UWIERZYTELNIANIE (JWT & Google)
// ==========================================
ConfigureAuthentication(builder);


// ==========================================
// INFRASTRUKTURA (SignalR, CORS, Cache, Limity)
// ==========================================
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


ConfigureCors(builder);
ConfigureRateLimiter(builder);
ConfigureSwagger(builder);


builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"] ?? Environment.GetEnvironmentVariable("Sentry__Dsn");
    options.Debug = true;
    options.TracesSampleRate = 1.0; // Przechwytywanie z wydajności/performance'u
});

// ==========================================
// REJESTRACJA USŁUG (DI)
// ==========================================
RegisterBusinessServices(builder.Services);

// ==========================================
// PIPELINE APLIKACJI (Middleware)
// ==========================================
var app = builder.Build();

ConfigureMiddlewarePipeline(app);

// ==========================================
// INICJALIZACJA BAZY (Migracje i Seeding)
// ==========================================
await InitializeDatabase(app);

app.Run();









// ==========================================
// METODY POMOCNICZE                                         // TO DO przeniesc do pliku Extention
// ==========================================

void LoadDotEnv()
{
    var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
    string envFilePath = null;
    while (currentDirectory != null && !File.Exists(envFilePath = Path.Combine(currentDirectory.FullName, ".env")))
    {
        currentDirectory = currentDirectory.Parent;
    }

    if (envFilePath != null && File.Exists(envFilePath))
    {
        DotNetEnv.Env.Load(envFilePath);  //wczytuje zmienne z pliku .env
    }
    else
    {
        Console.WriteLine("OSTRZEŻENIE: Nie znaleziono pliku .env.");
    }
}

void ConfigureAuthentication(WebApplicationBuilder builder)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Sprawdza, czy token pochodzi od zaufanego serwera
            ValidateAudience = true, // Sprawdza, czy token jest przeznaczony dla naszej aplikacji
            ValidateLifetime = true, // Sprawdza, czy token nie wygasł
            ValidateIssuerSigningKey = true, // Sprawdza, czy klucz użyty do podpisania tokena jest prawidłowy

            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
              (path.StartsWithSegments("/chatHub") || path.StartsWithSegments("/notificationHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"];
    });
}



void ConfigureCors(WebApplicationBuilder builder)
{
    var frontendUrl = builder.Configuration["FRONTEND_BASE_URL"];
    var additionalOrigins = builder.Configuration["ALLOWED_ORIGINS"]?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? Array.Empty<string>();

    var allAllowedOrigins = additionalOrigins
        .Concat(new[] { frontendUrl })
        .Where(url => !string.IsNullOrEmpty(url))
        .Distinct()
        .ToArray();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allAllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}


void RegisterBusinessServices(IServiceCollection services)
{
    services.AddScoped<RoleSeeder>();
    services.AddScoped<GenreSeeder>();
    services.AddScoped<PlatformSeeder>();
    services.AddScoped<GameSeeder>();
    services.AddScoped<SeedManager>();

    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserManagementService, UserManagementService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IEmailSender, EmailSender>();
    services.AddScoped<IChatService, ChatService>();
    services.AddScoped<IHistoryLogService, HistoryLogService>();
    services.AddScoped<IGameService, GameService>();
    services.AddScoped<IFileService, CloudinaryService>();
    services.AddScoped<ICollectionService, CollectionService>();
    services.AddScoped<IFriendService, FriendService>();
    services.AddScoped<IStatsService, StatsService>();
}



void ConfigureSwagger(WebApplicationBuilder builder)
{

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Game Organizer Api", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
        });
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

}


void ConfigureRateLimiter(WebApplicationBuilder builder)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("LoginPolicy", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1); // Okno czasu
            opt.PermitLimit = 5;                  // Maksymalnie 5 żądań
            opt.QueueLimit = 0;                   // Brak kolejkowania
        });

        // limit liczony dla każdego urządzenia osobno
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 60, // 60 zapytań na minutę na 1 adres IP
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
}


void ConfigureMiddlewarePipeline(WebApplication app)
{
    // Konfiguracja do poprawnej obs�ugi za reverse proxy (na Render)
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();

    app.UseForwardedHeaders(forwardedHeadersOptions);

    // Polityka nagłówków
    var policyCollection = new HeaderPolicyCollection()
        .AddDefaultSecurityHeaders()
        .AddContentSecurityPolicy(csp =>
        {
            csp.AddDefaultSrc().Self();

            var connectSrc = csp.AddConnectSrc()
                .Self()
                .From("https://localhost:7128");

            var issuer = builder.Configuration["JWT:Issuer"];
            if (!string.IsNullOrEmpty(issuer))
            {
                connectSrc.From(issuer);
            }

            if (app.Environment.IsDevelopment())
            {

                csp.AddStyleSrc().Self().UnsafeInline();
                csp.AddScriptSrc().Self().UnsafeInline();
            }
            else
            {
                csp.AddStyleSrc().Self();
                csp.AddScriptSrc().Self();
            }
        })
        .AddCustomHeader("X-Permitted-Cross-Domain-Policies", "none")
        .AddPermissionsPolicy(p =>
        {
            p.AddCamera().None();
            p.AddMicrophone().None();
            p.AddGeolocation().None();
        })
        .RemoveServerHeader();

    // Middleware
    app.UseSecurityHeaders(policyCollection);

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //dla demonstracji odkomentować
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.UseStaticFiles();

    app.MapHealthChecks("/healthz");
    app.MapControllers();

    app.MapHub<ChatHub>("/chatHub");
    app.MapHub<NotificationHub>("/notificationHub");
}


async Task InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<GameOrganizerDbContext>();
    await ctx.Database.MigrateAsync();
    var seedManager = scope.ServiceProvider.GetRequiredService<SeedManager>();
    await seedManager.Seed();
}