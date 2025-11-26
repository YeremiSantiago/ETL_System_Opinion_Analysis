using OpinionAnalytics.Application.Interfaces;
using OpinionAnalytics.Application.Services;
using OpinionAnalytics.Domain.Interfaces;
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
                    _logger.LogInformation("🚀 Iniciando proceso ETL completo...");

                    using var scope = _serviceProvider.CreateScope();
                    var extractionService = scope.ServiceProvider.GetRequiredService<IDataExtractionService>();
                    var mappingService = scope.ServiceProvider.GetRequiredService<IDimensionMappingService>();
                    var dimensionRepository = scope.ServiceProvider.GetRequiredService<IDimensionLoadRepository>();

                 
                    if (!extractionService.ValidateDataSourcesConfiguration())
                    {
                        _logger.LogWarning("⚠️ Configuración inválida, esperando 5 minutos...");
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    
                    _logger.LogInformation("📥 Paso 1: Extrayendo datos de fuentes...");
                    var extractionResult = await extractionService.ExtractAllDataParallelAsync();

                    if (extractionResult.Status == "Error")
                    {
                        _logger.LogError("❌ Extracción falló: {Errors}", string.Join(", ", extractionResult.Errors));
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("✅ Extracción completada: {Records} registros, {Speed:F2} records/sec",
                        extractionResult.Metrics.TotalRecordsExtracted,
                        extractionResult.Metrics.RecordsPerSecond);

                  
                    _logger.LogInformation("🔄 Paso 2: Mapeando datos a dimensiones...");
                    var dimensionDto = mappingService.MapFromExtractionResult(extractionResult);

                    if (dimensionDto.ValidationErrors.Any())
                    {
                        _logger.LogWarning("⚠️ Errores en mapeo: {Errors}", string.Join(", ", dimensionDto.ValidationErrors));
                    }

                   
                    _logger.LogInformation("💾 Paso 3: Cargando dimensiones al DWH...");
                    var loadResult = await dimensionRepository.LoadDimensionsAsync(dimensionDto, stoppingToken);

                    if (loadResult.Errors.Any())
                    {
                        _logger.LogError("❌ Errores cargando dimensiones: {Errors}", string.Join(", ", loadResult.Errors));
                    }
                    else
                    {
                        _logger.LogInformation("✅ Dimensiones cargadas exitosamente:");
                        _logger.LogInformation("   📊 Clientes: +{New}/{Updated}", loadResult.ClientesInserted, loadResult.ClientesUpdated);
                        _logger.LogInformation("   📦 Productos: +{New}/{Updated}", loadResult.ProductosInserted, loadResult.ProductosUpdated);
                        _logger.LogInformation("   🔗 Fuentes: +{New}/{Updated}", loadResult.FuentesInserted, loadResult.FuentesUpdated);
                        _logger.LogInformation("   💭 Sentimientos: +{New}/{Updated}", loadResult.SentimientosInserted, loadResult.SentimientosUpdated);
                        _logger.LogInformation("   📅 Tiempos: +{New}/{Updated}", loadResult.TiemposInserted, loadResult.TiemposUpdated);
                    }

                   
                    var totalInserted = loadResult.ClientesInserted + loadResult.ProductosInserted + 
                                       loadResult.FuentesInserted + loadResult.SentimientosInserted + loadResult.TiemposInserted;
                    var totalUpdated = loadResult.ClientesUpdated + loadResult.ProductosUpdated + 
                                      loadResult.FuentesUpdated + loadResult.SentimientosUpdated + loadResult.TiemposUpdated;

                    _logger.LogInformation("🎯 ETL completado: {SourceRecords} → {TotalInserted} nuevos + {TotalUpdated} actualizados",
                        extractionResult.Metrics.TotalRecordsExtracted, totalInserted, totalUpdated);

                    
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error crítico en Worker Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}
