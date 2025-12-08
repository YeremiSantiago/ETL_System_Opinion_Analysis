using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionAnalytics.Domain.Configuration;
using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Dwh.Facts;
using OpinionAnalytics.Domain.Interfaces;
using OpinionAnalytics.Persistence.Repositories.Dwh.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Dwh
{
    public class FactLoadRepository : IFactLoadRepository
    {
        private readonly DwhDbContext _context;
        private readonly ILogger<FactLoadRepository> _logger;

        public FactLoadRepository(DwhDbContext context, ILogger<FactLoadRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CleanFactTableAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(" Iniciando limpieza de la tabla FactOpiniones...");

          
            var tableName = "Fact.FactOpiniones";

            try
            {
                await _context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {tableName}", cancellationToken);
                _logger.LogInformation(" Tabla {TableName} truncada exitosamente.", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al truncar. Intentando DELETE (más lento pero más seguro)...");
               
                await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {tableName}", cancellationToken);
                _logger.LogInformation(" Tabla {TableName} limpiada con DELETE.", tableName);
            }
        }

        public async Task<int> LoadFactsAsync(ExtractionResult extractionResult, CancellationToken cancellationToken = default)
        {
            if (extractionResult == null) throw new ArgumentNullException(nameof(extractionResult));

            _logger.LogInformation(" Preparando carga de hechos (Lookups de Dimensiones)...");

            var clientes = await _context.DimClientes.AsNoTracking()
                .ToDictionaryAsync(c => c.IdCliente.Trim().ToUpper(), c => c.ClienteKey, cancellationToken);

            var productos = await _context.DimProductos.AsNoTracking()
                .ToDictionaryAsync(p => p.IdProducto.Trim().ToUpper(), p => p.ProductoKey, cancellationToken);

            var fuentes = await _context.DimFuentes.AsNoTracking()
                .ToDictionaryAsync(f => f.IdFuente.Trim().ToUpper(), f => f.FuenteKey, cancellationToken);

            var sentimientos = await _context.DimSentimientos.AsNoTracking()
                .ToDictionaryAsync(s => s.Clasificacion.Trim().ToUpper(), s => s.SentimientoKey, cancellationToken);

            
            sentimientos.TryGetValue("NEUTRAL", out var neutralSentimientoKey);

            var facts = new List<FactOpiniones>();
            int skipped = 0;
            int skippedMissingSentimiento = 0;

            int GetKey(Dictionary<string, int> dict, string idRaw)
            {
                var key = (idRaw ?? "").Trim().ToUpper();
                if (dict.TryGetValue(key, out int val)) return val;
                return -1;
            }

            decimal MapSentimentToNumeric(string clasificacion)
            {
                if (string.IsNullOrWhiteSpace(clasificacion)) return 3m;
                var low = clasificacion.Trim().ToLowerInvariant();
                return low switch
                {
                    var s when s.StartsWith("pos") || s.Contains("pos") => 4m,
                    var s when s.StartsWith("neg") || s.Contains("neg") => 1m,
                    _ => 3m
                };
            }

            int GetTiempoKey(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

            string NormalizeProdId(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return "";
                var s = input.Trim();
                if (s.StartsWith("P", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
                return s.ToUpperInvariant();
            }

            // A. Encuestas CSV (LINQ)
            var csvFacts = extractionResult.EncuestasInternas?
                .Select(e =>
                {
                    var clienteKey = GetKey(clientes, e.IdCliente.ToString());
                    var productoKey = GetKey(productos, NormalizeProdId(e.IdProducto.ToString()));
                    var fuenteKey = GetKey(fuentes, "CSV");
                    var sentimientoKey = GetKey(sentimientos, (e.Clasificacion ?? "Neutral"));
                    var tiempoKey = GetTiempoKey(e.Fecha);

                    if (sentimientoKey <= 0)
                    {
                        if (neutralSentimientoKey > 0) sentimientoKey = neutralSentimientoKey;
                        else
                        {
                            skippedMissingSentimiento++;
                            return null;
                        }
                    }

                    decimal calificacion = e.PuntajeSatisfaccion switch
                    {
                        var v when v > 0 => (decimal)v,
                        _ => MapSentimentToNumeric(e.Clasificacion)
                    };

                    var f = new FactOpiniones
                    {
                        ClienteKey = clienteKey,
                        ProductoKey = productoKey,
                        FuenteKey = fuenteKey,
                        SentimientoKey = sentimientoKey,
                        TiempoKey = tiempoKey,
                        Calificacion = calificacion
                    };

                    return ValidateFact(f) ? f : null;
                })
                .Where(f => f != null)
                .Cast<FactOpiniones>()
                .ToList() ?? new List<FactOpiniones>();

            facts.AddRange(csvFacts);
            skipped += (extractionResult.EncuestasInternas?.Count() ?? 0) - csvFacts.Count;

            // B. Web Reviews (LINQ)
            var webFacts = extractionResult.WebReviews?
                .Select(r =>
                {
                    var idCliente = r.IdCliente.HasValue ? r.IdCliente.Value.ToString() : $"WEB_{(r.ClienteNombre ?? "").Trim()}";
                    var clienteKey = GetKey(clientes, idCliente);
                    var productoKey = GetKey(productos, NormalizeProdId(r.IdProducto.ToString()));
                    var fuenteKey = GetKey(fuentes, "SAOC_DB");
                    var sentClass = r.Rating >= 4 ? "Positivo" : (r.Rating <= 2 ? "Negativo" : "Neutral");
                    var sentimientoKey = GetKey(sentimientos, sentClass);
                    var tiempoKey = GetTiempoKey(r.Fecha);

                    if (sentimientoKey <= 0)
                    {
                        if (neutralSentimientoKey > 0) sentimientoKey = neutralSentimientoKey;
                        else
                        {
                            skippedMissingSentimiento++;
                            return null;
                        }
                    }

                    decimal calificacion = (decimal)r.Rating;

                    var f = new FactOpiniones
                    {
                        ClienteKey = clienteKey,
                        ProductoKey = productoKey,
                        FuenteKey = fuenteKey,
                        SentimientoKey = sentimientoKey,
                        TiempoKey = tiempoKey,
                        Calificacion = calificacion
                    };

                    return ValidateFact(f) ? f : null;
                })
                .Where(f => f != null)
                .Cast<FactOpiniones>()
                .ToList() ?? new List<FactOpiniones>();

            facts.AddRange(webFacts);
            skipped += (extractionResult.WebReviews?.Count() ?? 0) - webFacts.Count;

            // C. Social Comments (LINQ)
            var apiFacts = extractionResult.SocialComments?
                .Select(c =>
                {
                    var idCliente = c.IdCliente.HasValue ? c.IdCliente.Value.ToString() : $"API_{(c.ClienteNombre ?? "").Trim()}";
                    var clienteKey = GetKey(clientes, idCliente);
                    var productoKey = GetKey(productos, NormalizeProdId(c.IdProducto.ToString()));
                    var fuenteKey = GetKey(fuentes, "SOCIAL_API");

                    var inferred = "Neutral";
                    if (!string.IsNullOrWhiteSpace(c.Comentario))
                    {
                        var txt = c.Comentario.Trim().ToLowerInvariant();
                        if (txt.Contains("no") && (txt.Contains("recomiendo") || txt.Contains("malo") || txt.Contains("mal"))) inferred = "Negativo";
                        else if (txt.Contains("excelente") || txt.Contains("recomiendo") || txt.Contains("me encanta") || txt.Contains("bueno")) inferred = "Positivo";
                    }

                    var sentimientoKey = GetKey(sentimientos, inferred);
                    var tiempoKey = GetTiempoKey(c.Fecha);

                    if (sentimientoKey <= 0)
                    {
                        if (neutralSentimientoKey > 0) sentimientoKey = neutralSentimientoKey;
                        else
                        {
                            skippedMissingSentimiento++;
                            return null;
                        }
                    }

                    decimal calificacion = inferred == "Positivo" ? 4m : inferred == "Negativo" ? 1m : 3m;

                    var f = new FactOpiniones
                    {
                        ClienteKey = clienteKey,
                        ProductoKey = productoKey,
                        FuenteKey = fuenteKey,
                        SentimientoKey = sentimientoKey,
                        TiempoKey = tiempoKey,
                        Calificacion = calificacion
                    };

                    return ValidateFact(f) ? f : null;
                })
                .Where(f => f != null)
                .Cast<FactOpiniones>()
                .ToList() ?? new List<FactOpiniones>();

            facts.AddRange(apiFacts);
            skipped += (extractionResult.SocialComments?.Count() ?? 0) - apiFacts.Count;
            if (facts.Any())
            {
                await _context.FactOpiniones.AddRangeAsync(facts, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(" Insertados {Count} hechos. (Omitidos por falta de keys: {Skipped}, faltantes sentimiento: {SkippedSent})",
                    facts.Count, skipped, skippedMissingSentimiento);
                return facts.Count;
            }

            _logger.LogInformation(" No se insertaron hechos. Omitidos por falta de keys: {Skipped}", skipped);
            return 0;
        }

        private bool ValidateFact(FactOpiniones f)
        {
            return f.ClienteKey > 0 && f.ProductoKey > 0 && f.FuenteKey > 0 && f.TiempoKey > 0;
        }

        private int GetTiempoKey(DateTime date) => (date.Year * 10000) + (date.Month * 100) + date.Day;

        private string NormalizeProdId(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim();
            if (s.StartsWith("P", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
            return s.ToUpperInvariant();
        }

        private string NormalizarSentimiento(string s) => string.IsNullOrWhiteSpace(s) ? "Neutral" : s;
        private bool IsPositive(string s) => s?.ToLower().Contains("pos") ?? false;
    }
}
