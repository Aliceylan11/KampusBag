using System.Text;
using KampusBag.Core.Interfaces;
using KampusBag.Core.Options;
using KampusBag.Infrastructure.Persistence;
using KampusBag.Infrastructure.Services;
using KampusBag.WebAPI.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KampusBag.WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var builder = WebApplication.CreateBuilder(args);

        // ── Temel Servisler ───────────────────────────────────────────
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            // Swagger'da Bearer token test edebilmek için
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            });
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // ═══════════════════════════════════════════════════════════════
        // #3 SECRETS — Options Pattern
        // Hassas değerler user-secrets veya environment variable'dan gelir.
        // appsettings.json'daki değerler boş bırakıldı.
        //
        // Kurulum (bir kez terminalde çalıştır):
        //   cd KampusBag.WebAPI
        //   dotnet user-secrets init
        //   dotnet user-secrets set "Jwt:Key"                    "SUPER_SECRET_MIN_32_CHARS_HERE_!!"
        //   dotnet user-secrets set "EmailSettings:SenderEmail"   "your@gmail.com"
        //   dotnet user-secrets set "EmailSettings:SenderPassword" "gmail-app-password"
        //   dotnet user-secrets set "Encryption:Key"             "KampusBag@2025!SecureAES256Key#1"
        // ═══════════════════════════════════════════════════════════════
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"));

        // ═══════════════════════════════════════════════════════════════
        // #2 JWT Authentication
        // ═══════════════════════════════════════════════════════════════
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSection["Key"] ?? string.Empty;

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
                };

                // SignalR WebSocket bağlantısı için token query string'den okunur
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) &&
                            ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        // ── CORS — MAUI uygulaması Android + iOS/Windows'tan bağlanır ──
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MauiPolicy", policy =>
                policy
                    .WithOrigins(
                        "http://localhost:5178",
                        "http://10.0.2.2:5178",
                        "https://localhost:7129")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());   // SignalR için zorunlu
        });

        // ── SignalR (PascalCase JSON) ──────────────────────────────────
        builder.Services.AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment())
            .AddJsonProtocol(o => o.PayloadSerializerOptions.PropertyNamingPolicy = null);

        // ── Veritabanı ────────────────────────────────────────────────
        builder.Services.AddDbContext<KampusBagDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")));

        // ── Repository ───────────────────────────────────────────────
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // ═══════════════════════════════════════════════════════════════
        // #4 UYGULAMA SERVİSLERİ — Application katmanından
        // UserService → KampusBag.Application.Services (iş mantığı)
        // MessageService → Infrastructure (karmaşık SQL sorgular)
        // EmailService + TokenService → Infrastructure (dış servisler)
        // ═══════════════════════════════════════════════════════════════
        builder.Services.AddScoped<IUserService, KampusBag.Application.Services.UserService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IMessageService, MessageService>();
        builder.Services.AddScoped<ITokenService, TokenService>();

        // ── Build & Migration ────────────────────────────────────────
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var ctx = scope.ServiceProvider.GetRequiredService<KampusBagDbContext>();
                ctx.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Migration hatası: " + ex.Message);
            }
        }

        // ── Middleware pipeline ───────────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("MauiPolicy");          // CORS, auth'dan önce gelmeli
        app.UseAuthentication();            // #2 JWT
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat");  // SignalR hub

        app.Run();
    }
}
