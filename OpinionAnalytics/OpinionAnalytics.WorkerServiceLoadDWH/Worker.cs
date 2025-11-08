using OpinionAnalytics.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace OpinionAnalytics.WorkerServiceLoadDWH
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider; 

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider) 
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("🚀 Iniciando proceso ETL paralelo...");

                    using var scope = _serviceProvider.CreateScope();
                    var extractionService = scope.ServiceProvider.GetRequiredService<IDataExtractionService>();

                    if (!extractionService.ValidateDataSourcesConfiguration())
                    {
                        _logger.LogWarning("⚠️ Configuración inválida, esperando 5 minutos...");
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    var result = await extractionService.ExtractAllDataParallelAsync();

                    if (result.Status == "Success")
                    {
                        _logger.LogInformation("✅ ETL completado: {Records} registros, {Speed:F2} records/sec",
                            result.Metrics.TotalRecordsExtracted,
                            result.Metrics.RecordsPerSecond);
                    }
                    else if (result.Status == "Warning")
                    {
                        _logger.LogWarning("⚠️ ETL completado con advertencias: {Records} registros extraídos",
                            result.Metrics.TotalRecordsExtracted);
                        
                        if (result.Errors.Any())
                        {
                            _logger.LogWarning("Errores encontrados: {Errors}", string.Join(", ", result.Errors));
                        }
                    }
                    else
                    {
                        _logger.LogError("❌ ETL falló completamente: {Errors}", string.Join(", ", result.Errors));
                    }

                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error en Worker Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}
