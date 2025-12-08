using OpinionAnalytics.Persistence.Repositories.Db.Context;
using OpinionAnalytics.Persistence.Repositories.Dwh.Context;
using OpinionAnalytics.Domain.Configuration;
using OpinionAnalytics.Application.Services;
using OpinionAnalytics.Application.Interfaces;
using OpinionAnalytics.Domain.Interfaces;
using OpinionAnalytics.Persistence.Repositories.Db;
using OpinionAnalytics.Persistence.Repositories.Api;
using OpinionAnalytics.Persistence.Repositories.Csv;
using OpinionAnalytics.Persistence.Repositories.Dwh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OpinionAnalytics.WorkerServiceLoadDWH
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

            var builder = Host.CreateApplicationBuilder(args);

            
            builder.Services.Configure<DataSourcesConfiguration>(
                builder.Configuration.GetSection(DataSourcesConfiguration.SectionName));
            
            builder.Services.Configure<ETLConfiguration>(
                builder.Configuration.GetSection(ETLConfiguration.SectionName));

            
            builder.Services.AddHttpClient();

            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SAOC_Database")));

            builder.Services.AddDbContext<DwhDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DWH_Database")));

         
            builder.Services.AddScoped<IEncuestaInternaCsvExtractorRepository, EncuestaInternaCsvExtractorRepository>();
            builder.Services.AddScoped<IWebReviewDbExtractorRepository, WebReviewDbExtractorRepository>();
            builder.Services.AddScoped<ISocialCommentApiExtractorRepository, SocialCommentApiExtractorRepository>();

           
            builder.Services.AddScoped<IDimensionLoadRepository, DimensionLoadRepository>();
            builder.Services.AddScoped<IFactLoadRepository, FactLoadRepository>();

            builder.Services.AddScoped<IDataExtractionService, DataExtractionService>();
            builder.Services.AddScoped<IDimensionMappingService, DimensionMappingService>();

            // Worker Service
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}