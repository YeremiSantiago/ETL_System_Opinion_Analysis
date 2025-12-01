using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionAnalytics.Application.Interfaces;
using OpinionAnalytics.Domain.Configuration;
using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Api;
using OpinionAnalytics.Domain.Entities.Csv;
using OpinionAnalytics.Domain.Entities.Db;
using OpinionAnalytics.Domain.Interfaces;
using OpinionAnalytics.Persistence.Repositories.Api;
using OpinionAnalytics.Persistence.Repositories.Csv;
using System.Diagnostics;

namespace OpinionAnalytics.Application.Services
{
    public class DataExtractionService : IDataExtractionService
    {
        private readonly IEncuestaInternaCsvExtractorRepository _csvRepository;
        private readonly IWebReviewDbExtractorRepository _dbRepository;
        private readonly ISocialCommentApiExtractorRepository _apiRepository;
        private readonly ILogger<DataExtractionService> _logger;
        private readonly DataSourcesConfiguration _dataSourcesConfig;
        private readonly ETLConfiguration _etlConfig;

        public DataExtractionService(
            IEncuestaInternaCsvExtractorRepository csvRepository,
            IWebReviewDbExtractorRepository dbRepository,
            ISocialCommentApiExtractorRepository apiRepository,
            ILogger<DataExtractionService> logger,
            IOptions<DataSourcesConfiguration> dataSourcesConfig,
            IOptions<ETLConfiguration> etlConfig)
        {
            _csvRepository = csvRepository;
            _dbRepository = dbRepository;
            _apiRepository = apiRepository;
            _logger = logger;
            _dataSourcesConfig = dataSourcesConfig.Value;
            _etlConfig = etlConfig.Value;
        }

        public async Task<IEnumerable<EncuestaInterna>> ExtractCsvDataAsync()
        {
            return await ExtractCsvDataAsync(_dataSourcesConfig.CsvFilePath);
        }

        public async Task<IEnumerable<EncuestaInterna>> ExtractCsvDataAsync(string csvFilePath)
        {
            try
            {
                _logger.LogInformation("Extrayendo datos CSV desde: {FilePath}", csvFilePath);

                var stopwatch = Stopwatch.StartNew();
                var data = await _csvRepository.LoadFromCsvAsync(csvFilePath);
                stopwatch.Stop();

                _logger.LogInformation("CSV extraído: {Count} registros en {Time}ms",
                    data.Count(), stopwatch.ElapsedMilliseconds);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos CSV desde {FilePath}", csvFilePath);
                throw;
            }
        }

        public async Task<IEnumerable<WebReviewView>> ExtractDatabaseDataAsync()
        {
            try
            {
                _logger.LogInformation("Extrayendo datos de base de datos SAOC...");

                var stopwatch = Stopwatch.StartNew();
                var data = await _dbRepository.GetForDimensionLoadingAsync(); 
                stopwatch.Stop();

                _logger.LogInformation("BD extraída: {Count} registros en {Time}ms",
                    data.Count(), stopwatch.ElapsedMilliseconds);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de BD SAOC");
                throw;
            }
        }

        public async Task<IEnumerable<SocialCommentView>> ExtractApiDataAsync()
        {
            try
            {
                _logger.LogInformation("Extrayendo datos de API desde: {ApiUrl}", 
                    _dataSourcesConfig.ApiSettings.SocialCommentsUrl);

                var stopwatch = Stopwatch.StartNew();
                var data = await _apiRepository.GetAllAsync();
                stopwatch.Stop();

                _logger.LogInformation("API extraída: {Count} registros en {Time}ms",
                    data.Count(), stopwatch.ElapsedMilliseconds);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo datos de API");
                throw;
            }
        }

        public async Task<IEnumerable<EncuestaInterna>> ExtractCsvByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await ExtractCsvByDateRangeAsync(_dataSourcesConfig.CsvFilePath, startDate, endDate);
        }

        public async Task<IEnumerable<EncuestaInterna>> ExtractCsvByDateRangeAsync(string csvFilePath, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Extrayendo CSV por rango de fechas: {StartDate} - {EndDate}", startDate, endDate);

                await _csvRepository.LoadFromCsvAsync(csvFilePath);
                var data = await _csvRepository.GetByDateRangeAsync(startDate, endDate);

                _logger.LogInformation("CSV filtrado: {Count} registros", data.Count());

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo CSV por fechas");
                throw;
            }
        }

        public async Task<IEnumerable<WebReviewView>> ExtractDatabaseByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Extrayendo BD por rango de fechas: {StartDate} - {EndDate}", startDate, endDate);

                var data = await _dbRepository.GetByDateRangeAsync(startDate, endDate);

                _logger.LogInformation("BD filtrada: {Count} registros", data.Count());

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo BD por fechas");
                throw;
            }
        }

        public async Task<IEnumerable<SocialCommentView>> ExtractApiByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Extrayendo API por rango de fechas: {StartDate} - {EndDate}", startDate, endDate);

                var data = await _apiRepository.GetByDateRangeAsync(startDate, endDate);

                _logger.LogInformation("API filtrada: {Count} registros", data.Count());

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo API por fechas");
                throw;
            }
        }

        public async Task<ExtractionMetrics> GetExtractionMetricsAsync()
        {
            var metrics = new ExtractionMetrics();

            try
            {
                _logger.LogInformation("Obteniendo métricas de extracción...");

                var stopwatchCsv = Stopwatch.StartNew();
                metrics.CsvRecordsCount = await _csvRepository.CountAsync();
                stopwatchCsv.Stop();
                metrics.CsvExtractionTime = stopwatchCsv.Elapsed;

                var stopwatchDb = Stopwatch.StartNew();
                metrics.DatabaseRecordsCount = await _dbRepository.CountAsync();
                stopwatchDb.Stop();
                metrics.DatabaseExtractionTime = stopwatchDb.Elapsed;

                var stopwatchApi = Stopwatch.StartNew();
                metrics.ApiRecordsCount = await _apiRepository.CountAsync();
                stopwatchApi.Stop();
                metrics.ApiExtractionTime = stopwatchApi.Elapsed;

                if (metrics.CsvRecordsCount > 0) metrics.ExtractedSources.Add("CSV");
                if (metrics.DatabaseRecordsCount > 0) metrics.ExtractedSources.Add("Database");
                if (metrics.ApiRecordsCount > 0) metrics.ExtractedSources.Add("API");

                _logger.LogInformation("Métricas obtenidas - Total: {Total} registros de {Sources} fuentes",
                    metrics.TotalRecordsExtracted, metrics.ExtractedSources.Count);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo métricas de extracción");
                throw;
            }
        }

        public bool ValidateDataSourcesConfiguration()
        {
            var isValid = true;

            if (string.IsNullOrEmpty(_dataSourcesConfig.CsvFilePath))
            {
                _logger.LogWarning("Ruta de archivo CSV no configurada");
                isValid = false;
            }
            else if (!File.Exists(_dataSourcesConfig.CsvFilePath))
            {
                _logger.LogWarning("Archivo CSV no encontrado: {FilePath}", _dataSourcesConfig.CsvFilePath);
                isValid = false;
            }
            else
            {
                _logger.LogInformation("Archivo CSV encontrado: {FilePath}", _dataSourcesConfig.CsvFilePath);
            }

            return isValid;
        }

        public async Task<ExtractionResult> ExtractAllDataParallelAsync()
        {
            var result = new ExtractionResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("🚀 Iniciando extracción PARALELA de todas las fuentes...");

               
                var csvTask = Task.Run(async () => 
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        result.EncuestasInternas = await ExtractCsvDataAsync();
                        result.Metrics.CsvExtractionTime = sw.Elapsed;
                        result.Metrics.CsvRecordsCount = result.EncuestasInternas.Count();
                        _logger.LogInformation("✅ CSV completado: {Count} registros", result.Metrics.CsvRecordsCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error extrayendo CSV - continuando sin datos CSV");
                        result.EncuestasInternas = new List<EncuestaInterna>();
                        result.Errors.Add($"CSV: {ex.Message}");
                    }
                });

                var dbTask = Task.Run(async () => 
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        result.WebReviews = await ExtractDatabaseDataAsync();
                        result.Metrics.DatabaseExtractionTime = sw.Elapsed;
                        result.Metrics.DatabaseRecordsCount = result.WebReviews.Count();
                        _logger.LogInformation("✅ BD completada: {Count} registros", result.Metrics.DatabaseRecordsCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error extrayendo BD - continuando sin datos BD");
                        result.WebReviews = new List<WebReviewView>();
                        result.Errors.Add($"BD: {ex.Message}");
                    }
                });

                var apiTask = Task.Run(async () => 
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        result.SocialComments = await ExtractApiDataAsync();
                        result.Metrics.ApiExtractionTime = sw.Elapsed;
                        result.Metrics.ApiRecordsCount = result.SocialComments.Count();
                        _logger.LogInformation("✅ API completada: {Count} registros", result.Metrics.ApiRecordsCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error extrayendo API - continuando sin datos API");
                        result.SocialComments = new List<SocialCommentView>();
                        result.Errors.Add($"API: {ex.Message}");
                    }
                });

                // ✅ NUEVAS TAREAS MAESTRAS
                var productosTask = Task.Run(async () => 
                {
                    try
                    {
                        result.ProductosMaestros = await ExtractProductosMaestrosAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error con productos maestros");
                        result.ProductosMaestros = new List<ProductoCsv>();
                        result.Errors.Add($"ProductosMaestros: {ex.Message}");
                    }
                });

                var clientesTask = Task.Run(async () => 
                {
                    try
                    {
                        result.ClientesMaestros = await ExtractClientesMaestrosAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error con clientes maestros");
                        result.ClientesMaestros = new List<ClienteCsv>();
                        result.Errors.Add($"ClientesMaestros: {ex.Message}");
                    }
                });

                // ✅ ESPERAR TODAS LAS TAREAS
                await Task.WhenAll(csvTask, dbTask, apiTask, productosTask, clientesTask);

                stopwatch.Stop();
                result.Metrics.TotalExtractionTime = stopwatch.Elapsed;
                
                
                if (result.Metrics.CsvRecordsCount > 0) result.Metrics.ExtractedSources.Add("CSV");
                if (result.Metrics.DatabaseRecordsCount > 0) result.Metrics.ExtractedSources.Add("Database");
                if (result.Metrics.ApiRecordsCount > 0) result.Metrics.ExtractedSources.Add("API");
                
                result.Metrics.ApiCallsCount = 1;
                
               
                if (result.Metrics.ExtractedSources.Any())
                {
                    result.Status = "Success";
                    _logger.LogInformation("✅ Extracción PARALELA completada: {TotalRecords} registros de {Sources} fuentes", 
                        result.Metrics.TotalRecordsExtracted, string.Join(", ", result.Metrics.ExtractedSources));
                }
                else
                {
                    result.Status = "Warning";
                    _logger.LogWarning("⚠️ Extracción completada pero sin datos de ninguna fuente");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico durante extracción paralela");
                result.Status = "Error";
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        public async Task RefreshApiCacheAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Refrescando cache de API...");
                
                
                await _apiRepository.RefreshCacheAsync();
                
                _logger.LogInformation("✅ Cache de API refrescado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error refrescando cache de API");
                throw;
            }
        }

        // Nuevos métodos para extraer datos maestros
        public async Task<IEnumerable<ProductoCsv>> ExtractProductosMaestrosAsync()
        {
            try
            {
                var productoCsvPath = Path.Combine(Path.GetDirectoryName(_dataSourcesConfig.CsvFilePath) ?? "", "productos.csv");
                
                _logger.LogInformation("Extrayendo productos maestros desde: {FilePath}", productoCsvPath);

                if (!File.Exists(productoCsvPath))
                {
                    _logger.LogWarning("Archivo de productos maestros no encontrado: {Path}", productoCsvPath);
                    return new List<ProductoCsv>();
                }

                var productos = new List<ProductoCsv>();
                var lines = await File.ReadAllLinesAsync(productoCsvPath);
                
                foreach (var line in lines.Skip(1)) // Saltar header
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out var id))
                    {
                        productos.Add(new ProductoCsv
                        {
                            IdProducto = id,
                            Nombre = parts[1].Trim('"'),
                            Categoria = parts[2].Trim('"')
                        });
                    }
                }

                _logger.LogInformation("✅ Productos maestros cargados: {Count} registros", productos.Count);
                return productos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando productos maestros");
                return new List<ProductoCsv>();
            }
        }

        public async Task<IEnumerable<ClienteCsv>> ExtractClientesMaestrosAsync()
        {
            try
            {
                var clienteCsvPath = Path.Combine(Path.GetDirectoryName(_dataSourcesConfig.CsvFilePath) ?? "", "clientes.csv");
                
                _logger.LogInformation("Extrayendo clientes maestros desde: {FilePath}", clienteCsvPath);

                if (!File.Exists(clienteCsvPath))
                {
                    _logger.LogWarning("Archivo de clientes maestros no encontrado: {Path}", clienteCsvPath);
                    return new List<ClienteCsv>();
                }

                var clientes = new List<ClienteCsv>();
                var lines = await File.ReadAllLinesAsync(clienteCsvPath);
                
                foreach (var line in lines.Skip(1)) // Saltar header
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out var id))
                    {
                        clientes.Add(new ClienteCsv
                        {
                            IdCliente = id,
                            Nombre = parts[1].Trim('"'),
                            Email = parts[2].Trim('"'),
                            Telefono = parts.Length > 3 ? parts[3].Trim('"') : null,
                            Ciudad = parts.Length > 4 ? parts[4].Trim('"') : null,
                            FechaRegistro = parts.Length > 5 && DateTime.TryParse(parts[5], out var fecha) ? fecha : null
                        });
                    }
                }

                _logger.LogInformation("✅ Clientes maestros cargados: {Count} registros", clientes.Count);
                return clientes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando clientes maestros");
                return new List<ClienteCsv>();
            }
        }
    }
}
