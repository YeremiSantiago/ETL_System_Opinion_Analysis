using OpinionAnalytics.Application.Interfaces;
using OpinionAnalytics.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

                    var factRepository = scope.ServiceProvider.GetRequiredService<IFactLoadRepository>();

                    if (!extractionService.ValidateDataSourcesConfiguration())
                    {
                        _logger.LogWarning("⚠️ Configuración inválida, esperando 5 minutos...");
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    // --- PASO 1: EXTRACCIÓN ---
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

                    // --- PASO 2: MAPEO A DIMENSIONES ---
                    _logger.LogInformation("🔄 Paso 2: Mapeando datos a dimensiones...");
                    var dimensionDto = mappingService.MapFromExtractionResult(extractionResult);

                    if (dimensionDto.ValidationErrors.Any())
                    {
                        _logger.LogWarning("⚠️ Errores en mapeo: {Errors}", string.Join(", ", dimensionDto.ValidationErrors));
                    }

                    // --- PASO 3: CARGA DE DIMENSIONES ---
                    _logger.LogInformation("💾 Paso 3: Cargando dimensiones al DWH...");
                    var loadResult = await dimensionRepository.LoadDimensionsAsync(dimensionDto, stoppingToken);

                    if (loadResult.Errors.Any())
                    {
                        _logger.LogError("❌ Errores cargando dimensiones: {Errors}. Se aborta la carga de hechos.", string.Join(", ", loadResult.Errors));
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }
                    else
                    {
                        LogDimensionResults(loadResult);
                    }

                    // --- PASO 4: CARGA DE HECHOS (FACT TABLE) ---
                    _logger.LogInformation("📊 Paso 4: Procesando Tabla de Hechos (FactOpiniones)...");

                    try
                    {
                     
                        await factRepository.CleanFactTableAsync(stoppingToken);

                         var factsInserted = await factRepository.LoadFactsAsync(extractionResult, stoppingToken);

                        var totalInsertedDims = loadResult.ClientesInserted + loadResult.ProductosInserted +
                                                loadResult.FuentesInserted + loadResult.SentimientosInserted + loadResult.TiemposInserted;

                        _logger.LogInformation("🎯 ETL COMPLETADO EXITOSAMENTE");
                        _logger.LogInformation("   Resumen: {SourceRecords} registros origen -> {Facts} hechos cargados.",
                            extractionResult.Metrics.TotalRecordsExtracted, factsInserted);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error crítico durante la carga de hechos.");
                    }

                  
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error crítico no controlado en Worker Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private void LogDimensionResults(Domain.Dtos.DimensionLoadResult loadResult)
        {
            _logger.LogInformation("✅ Dimensiones sincronizadas:");
            _logger.LogInformation("   busts Clientes: +{New}/{Updated}", loadResult.ClientesInserted, loadResult.ClientesUpdated);
            _logger.LogInformation("   📦 Productos: +{New}/{Updated}", loadResult.ProductosInserted, loadResult.ProductosUpdated);
            _logger.LogInformation("   🔗 Fuentes: +{New}/{Updated}", loadResult.FuentesInserted, loadResult.FuentesUpdated);
            _logger.LogInformation("   💭 Sentimientos: +{New}/{Updated}", loadResult.SentimientosInserted, loadResult.SentimientosUpdated);
            _logger.LogInformation("   📅 Tiempos: +{New}/{Updated}", loadResult.TiemposInserted, loadResult.TiemposUpdated);
        }
    }
}
