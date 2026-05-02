using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Persistence;
using KampusBag.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

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

        // ── Veritabanı ────────────────────────────────────────────────
        builder.Services.AddDbContext<KampusBagDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")));

        // ── Repository ───────────────────────────────────────────────
        builder.Services.AddScoped(
            typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // ── Servisler ────────────────────────────────────────────────
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IMessageService, MessageService>();  // YENİ

        // ── Otomatik Migration ───────────────────────────────────────
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var ctx = scope.ServiceProvider
                    .GetRequiredService<KampusBagDbContext>();
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

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
