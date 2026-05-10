using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Persistence;
using KampusBag.Infrastructure.Services;
using KampusBag.WebAPI.Hubs;
using Microsoft.EntityFrameworkCore;

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
        builder.Services.AddSwaggerGen();

        // ── CORS — MAUI uygulaması hem Android hem iOS/Windows'tan bağlanır ──
        // SignalR WebSocket bağlantısı AllowCredentials() gerektirir,
        // bu yüzden AllowAnyOrigin() kullanılamaz; originler açıkça belirtilmeli.
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MauiPolicy", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5178",       // iOS / Windows MAUI
                        "http://10.0.2.2:5178",        // Android emülatör
                        "https://localhost:7129")       // HTTPS profili
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();               // SignalR için zorunlu
            });
        });

        // ── SignalR ───────────────────────────────────────────────────
        // PascalCase JSON: MAUI model sınıfları PascalCase, SignalR varsayılanı camelCase.
        // PropertyNamingPolicy = null → sunucu PascalCase gönderir,
        // MAUI SignalR istemcisi PropertyNameCaseInsensitive=true ile alır.
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        })
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
        });

        // ── Veritabanı ────────────────────────────────────────────────
        builder.Services.AddDbContext<KampusBagDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")));

        // ── Repository ───────────────────────────────────────────────
        builder.Services.AddScoped(
            typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // ── Uygulama Servisleri ───────────────────────────────────────
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IMessageService, MessageService>();

        // ── Otomatik Migration ───────────────────────────────────────
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

        // ── Middleware ────────────────────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // app.UseHttpsRedirection();

        // CORS, routing'den ÖNCE gelmeli
        app.UseCors("MauiPolicy");

        app.UseAuthorization();
        app.MapControllers();

        // ── SignalR Hub ───────────────────────────────────────────────
        app.MapHub<ChatHub>("/hubs/chat");

        app.Run();
    }
}
