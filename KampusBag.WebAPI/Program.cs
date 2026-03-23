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
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<KampusBagDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IMessageService, MessageService>();

            var app = builder.Build();

            // ==============================================================
            // MIGRATION KODU BURADA, YANİ PIPELINE AYARLARINDAN ÖNCE OLMALI!
            // ==============================================================
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<KampusBagDbContext>();
                    context.Database.Migrate(); // Veritabanı ve tabloları oluştur
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Veritabanı oluşturulurken hata: " + ex.Message);
                }
            }
            // ==============================================================

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
}