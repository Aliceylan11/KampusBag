using KampusBag.Application.Services;
using KampusBag.Core.Interfaces;
using KampusBag.Core.Options;
using KampusBag.Infrastructure.Persistence;
using KampusBag.Infrastructure.Services;
using KampusBag.WebAPI.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace KampusBag.WebAPI;

public class Program
{
    public static void Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Options
        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"));

        // JWT
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new Exception("Jwt:Key eksik. dotnet user-secrets set 'Jwt:Key' '...' çalıştırın.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/hubs"))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        // CORS — Ngrok + fiziksel cihaz desteği
        // FIX: SetIsOriginAllowedToAllOrigins() yok, doğru metot SetIsOriginAllowed(_ => true)
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MauiPolicy", policy =>
            {
                policy
                    .SetIsOriginAllowed(_ => true)  // tüm origin'lere izin ver
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // SignalR
        builder.Services.AddSignalR(opt => opt.EnableDetailedErrors = builder.Environment.IsDevelopment())
            .AddJsonProtocol(opt => opt.PayloadSerializerOptions.PropertyNamingPolicy = null);

        // DB
        builder.Services.AddDbContext<KampusBagDbContext>(opt =>
            opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // DI
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IMessageService, MessageService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            try { scope.ServiceProvider.GetRequiredService<KampusBagDbContext>().Database.Migrate(); }
            catch (Exception ex) { Console.WriteLine($"[Migration] {ex.Message}"); }
        }

        if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

        //app.UseHttpsRedirection();
        app.UseCors("MauiPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<ChatHub>("/hubs/chat");
        app.Run();
    }
}