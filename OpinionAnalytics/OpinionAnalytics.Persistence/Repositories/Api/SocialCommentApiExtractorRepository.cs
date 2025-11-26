using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Api;
using System.Text.Json;

namespace OpinionAnalytics.Persistence.Repositories.Api
{
    public class SocialCommentApiExtractorRepository : ISocialCommentApiExtractorRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialCommentApiExtractorRepository> _logger;
        private List<SocialCommentView>? _cachedComments = null;
        private DateTime? _lastCacheTime = null;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public SocialCommentApiExtractorRepository(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<SocialCommentApiExtractorRepository> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<SocialCommentView>> GetAllAsync()
        {
            await EnsureDataLoadedAsync();
            return _cachedComments ?? new List<SocialCommentView>();
        }

        public async Task<SocialCommentView?> GetByIdAsync(object id)
        {
            await EnsureDataLoadedAsync();
            return _cachedComments?.FirstOrDefault(c => c.IdComment == (int)id);
        }

        public async Task<IEnumerable<SocialCommentView>> GetByConditionAsync(Func<SocialCommentView, bool> predicate)
        {
            await EnsureDataLoadedAsync();
            return _cachedComments?.Where(predicate) ?? new List<SocialCommentView>();
        }

        public async Task<int> CountAsync()
        {
            await EnsureDataLoadedAsync();
            return _cachedComments?.Count ?? 0;
        }

        public async Task<bool> ExistsAsync(object id)
        {
            var comment = await GetByIdAsync(id);
            return comment != null;
        }

        public async Task<IEnumerable<SocialCommentView>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Filtrando localmente por fechas: {StartDate} - {EndDate}", startDate, endDate);
            return await GetByConditionAsync(s => s.Fecha >= startDate && s.Fecha <= endDate);
        }

        public async Task<IEnumerable<SocialCommentView>> GetBySourceAsync(string source)
        {
            _logger.LogInformation("Filtrando localmente por fuente: {Source}", source);
            return await GetByConditionAsync(s => s.Fuente.Contains(source, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IEnumerable<SocialCommentView>> GetByProductAsync(int productId)
        {
            _logger.LogInformation("Filtrando localmente por producto: {ProductId}", productId);
            return await GetByConditionAsync(s => s.IdProducto == productId);
        }

        private async Task EnsureDataLoadedAsync()
        {
            if (_cachedComments == null || 
                _lastCacheTime == null || 
                DateTime.UtcNow - _lastCacheTime > _cacheExpiration)
            {
                await LoadFromApiAsync();
            }
        }

        private async Task LoadFromApiAsync()
        {
            try
            {
                _logger.LogInformation("🌐 Intentando conectar a API: {ApiUrl}", _configuration["DataSources:ApiSettings:SocialCommentsUrl"]);
                
                var apiUrl = _configuration["DataSources:ApiSettings:SocialCommentsUrl"];
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var response = await _httpClient.GetStringAsync(apiUrl);

               
                var dtoComments = JsonSerializer.Deserialize<List<SocialCommentExtractionDto>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var mapped = dtoComments?.Select(c => new SocialCommentView
                {
                    IdComment = c.IdComment,
                    IdCliente = c.IdCliente,
                    IdProducto = c.IdProducto,
                    Fuente = c.Fuente,
                    Fecha = c.Fecha,
                    Comentario = c.Comentario,
                    ClienteNombre = c.ClienteNombre,
                    ClienteEmail = c.ClienteEmail,
                    ProductoNombre = c.ProductoNombre,
                    ProductoCategoria = c.ProductoCategoria,
                    TipoFuente = c.TipoFuente
                }).ToList();

                stopwatch.Stop();
                
                _cachedComments = mapped ?? new List<SocialCommentView>();
                _lastCacheTime = DateTime.UtcNow;
                
                _logger.LogInformation("✅ API cargada: {Count} registros en {Time}ms", 
                    _cachedComments.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("⚠️ API no disponible: {Message}. Continuando sin datos de API.", ex.Message);
                _cachedComments = new List<SocialCommentView>();
                _lastCacheTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado cargando comentarios desde API");
                _cachedComments = new List<SocialCommentView>();
                _lastCacheTime = DateTime.UtcNow;
            }
        }

        public async Task RefreshCacheAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Forzando recarga de cache de API");
                _cachedComments?.Clear();
                await LoadFromApiAsync(); 
                _logger.LogInformation("✅ Cache de API refrescado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error refrescando cache de API");
                throw;
            }
        }
    }
}
