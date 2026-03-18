using KampusBag.Core.Interfaces;
using KampusBag.Infrastructure.Persistence;
using KampusBag.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace KampusBag.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. PostgreSQL Tarih Ayarı
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            // 2. Servis Kayıtları
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Veritabanı Bağlantısı
            builder.Services.AddDbContext<KampusBagDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Dependency Injection (DI) Kayıtları
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IMessageService, MessageService>();

            var app = builder.Build();

            // 3. Pipeline (Akış) Ayarları
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            // ==============================================================
            // İŞTE EKSİK OLAN VE VERİTABANINI OLUŞTURACAK O SİHİRLİ KOD BURASI
            // ==============================================================
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<KampusBagDbContext>();
                    // Bu komut, veritabanı yoksa oluşturur, tabloları eksiksiz ekler.
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Veritabanı oluşturulurken hata: " + ex.Message);
                }
            }
            // ==============================================================

            app.Run();
        }
    }
}