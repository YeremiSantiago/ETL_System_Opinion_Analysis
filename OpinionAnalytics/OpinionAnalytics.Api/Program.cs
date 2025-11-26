using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Api.Data.Context;

namespace OpinionAnalytics.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddDbContext<ApiDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SAOC_Database")));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "Opinion Analytics API",
                    Version = "v1",
                    Description = "API para extraer comentarios de redes sociales"
                });
            });

            var app = builder.Build();

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
