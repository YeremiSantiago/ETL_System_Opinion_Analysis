using Microsoft.Extensions.Logging;
using OpinionAnalytics.Application.Interfaces;
using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Dwh.Dimensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OpinionAnalytics.Application.Services
{
    public class DimensionMappingService : IDimensionMappingService
    {
        private readonly ILogger<DimensionMappingService> _logger;

        public DimensionMappingService(ILogger<DimensionMappingService> logger)
        {
            _logger = logger;
        }

        public DimensionLoadDto MapFromExtractionResult(ExtractionResult extractionResult)
        {
            var dto = new DimensionLoadDto
            {
                ProcessedAt = DateTime.UtcNow,
                TotalSourceRecords = extractionResult.Metrics.TotalRecordsExtracted,
                ProcessedSources = extractionResult.Metrics.ExtractedSources.ToList()
            };

            try
            {
                dto.Clientes = MapClientes(extractionResult);
                dto.Productos = MapProductos(extractionResult);
                dto.Fuentes = MapFuentes(extractionResult);
                dto.Sentimientos = MapSentimientos(extractionResult);
                dto.Tiempos = MapTiempos(extractionResult);

                _logger.LogInformation("Mapeo completado: {Clientes} clientes, {Productos} productos, {Fuentes} fuentes, {Sentimientos} sentimientos, {Tiempos} fechas",
                    dto.Clientes.Count, dto.Productos.Count, dto.Fuentes.Count, dto.Sentimientos.Count, dto.Tiempos.Count);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en mapeo de dimensiones");
                dto.ValidationErrors.Add($"Error en mapeo: {ex.Message}");
                return dto;
            }
        }

        #region Mappers por Dimensión

        private List<DimCliente> MapClientes(ExtractionResult extractionResult)
        {
            var clientesCsv = extractionResult.EncuestasInternas
               .Select(e => new DimCliente
               {
                   IdCliente = e.IdCliente.ToString(),
                   Nombre = $"Cliente {e.IdCliente}",
                   Email = null
               });

            var clientesDb = extractionResult.WebReviews
                .Select(r =>
                {
                    var id = r.IdCliente.HasValue ? r.IdCliente.Value.ToString() : $"WEB_{(r.ClienteNombre ?? Guid.NewGuid().ToString()).Trim()}";
                    return new DimCliente
                    {
                        IdCliente = id,
                        Nombre = !string.IsNullOrWhiteSpace(r.ClienteNombre) ? r.ClienteNombre.Trim() : $"Cliente_{id}",
                        Email = string.IsNullOrWhiteSpace(r.ClienteEmail) ? null : r.ClienteEmail.Trim().ToLowerInvariant()
                    };
                });

            var clientesApi = extractionResult.SocialComments
                .Select(c =>
                {
                    var id = c.IdCliente.HasValue ? c.IdCliente.Value.ToString() : $"API_{(c.ClienteNombre ?? Guid.NewGuid().ToString()).Trim()}";
                    return new DimCliente
                    {
                        IdCliente = id,
                        Nombre = !string.IsNullOrWhiteSpace(c.ClienteNombre) ? c.ClienteNombre.Trim() : $"Cliente_{id}",
                        Email = string.IsNullOrWhiteSpace(c.ClienteEmail) ? null : c.ClienteEmail.Trim().ToLowerInvariant()
                    };
                });

            var merged = clientesCsv
                .Concat(clientesDb)
                .Concat(clientesApi)
                .Where(c => !string.IsNullOrWhiteSpace(c.IdCliente))
                .GroupBy(c => c.IdCliente.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var withEmail = g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Email));
                    var candidate = withEmail ?? g.First();
                    if (string.IsNullOrWhiteSpace(candidate.Nombre))
                    {
                        candidate.Nombre = g.Select(x => x.Nombre).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? candidate.IdCliente;
                    }
                    return candidate;
                })
                .ToList();

            return merged;
        }

        private List<DimProducto> MapProductos(ExtractionResult extractionResult)
        {
            var raw = new List<(string IdProducto, string NombreProducto, string? Categoria, string Fuente)>();

            foreach (var encuesta in extractionResult.EncuestasInternas)
            {
                raw.Add((NormalizeProdId(encuesta.IdProducto.ToString()),
                         $"Producto {encuesta.IdProducto}",
                         null,
                         "CSV"));
            }

            foreach (var review in extractionResult.WebReviews)
            {
                raw.Add((NormalizeProdId(review.IdProducto.ToString()),
                         string.IsNullOrWhiteSpace(review.ProductoNombre) ? "Sin Nombre" : review.ProductoNombre.Trim(),
                         string.IsNullOrWhiteSpace(review.ProductoCategoria) ? null : review.ProductoCategoria.Trim(),
                         "WEB"));
            }

            foreach (var comment in extractionResult.SocialComments)
            {
                raw.Add((NormalizeProdId(comment.IdProducto.ToString()),
                         string.IsNullOrWhiteSpace(comment.ProductoNombre) ? "Sin Nombre" : comment.ProductoNombre.Trim(),
                         string.IsNullOrWhiteSpace(comment.ProductoCategoria) ? null : comment.ProductoCategoria.Trim(),
                         "API"));
            }

            var distinctProductIds = raw.Select(r => r.IdProducto).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            var productIdsWithAnyCategory = raw
                .Where(r => !string.IsNullOrWhiteSpace(r.Categoria))
                .Select(r => r.IdProducto)
                .Distinct()
                .ToList();

            _logger.LogInformation("MapProductos - DistinctProducts={Total}, ProductsWithAnyCategoryInSources={WithCat}", 
                distinctProductIds.Count, productIdsWithAnyCategory.Count);

             var ejemplos = productIdsWithAnyCategory.Take(20)
                .Select(id => new
                {
                    Id = id,
                    Detalle = raw.Where(r => r.IdProducto == id)
                                 .Select(r => $"{r.Fuente}:{(string.IsNullOrWhiteSpace(r.Categoria) ? "[no-cat]" : r.Categoria)}")
                                 .Distinct()
                                 .ToList()
                })
                .ToList();

            if (ejemplos.Any())
            {
                var lines = ejemplos.Select(e => $"{e.Id} => {string.Join(", ", e.Detalle)}");
                _logger.LogDebug("MapProductos - ejemplos de productos con categoría en fuentes (hasta 20): {Lines}", string.Join(" | ", lines));
            }

            var productIdsWithoutCategory = distinctProductIds.Except(productIdsWithAnyCategory).Take(20).ToList();
            if (productIdsWithoutCategory.Any())
            {
                _logger.LogDebug("MapProductos - ejemplos de productos SIN categoría en fuentes (hasta 20): {Ids}", string.Join(", ", productIdsWithoutCategory));
            }

            var grouped = raw
                .Where(r => !string.IsNullOrWhiteSpace(r.IdProducto))
                .GroupBy(r => r.IdProducto, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = new List<DimProducto>();
            int categoriaEncontrada = 0;
            int categoriaPerdida = 0;

            foreach (var g in grouped)
            {
                var entries = g.ToList();

                var specific = entries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Categoria) && !IsGenericCategory(e.Categoria));
                if (specific.IdProducto != null && specific.Categoria != null)
                {
                    result.Add(new DimProducto
                    {
                        IdProducto = specific.IdProducto,
                        NombreProducto = GetBestProductName(entries),
                        Categoria = specific.Categoria,
                        Subcategoria = null
                    });
                    categoriaEncontrada++;
                    continue;
                }

                var withCategory = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Categoria))
                    .OrderBy(e => GetSourcePriority(e.Fuente))
                    .FirstOrDefault();

                if (withCategory.IdProducto != null && withCategory.Categoria != null)
                {
                    result.Add(new DimProducto
                    {
                        IdProducto = withCategory.IdProducto,
                        NombreProducto = GetBestProductName(entries),
                        Categoria = withCategory.Categoria,
                        Subcategoria = null
                    });
                    categoriaEncontrada++;
                    continue;
                }

                var first = entries.First();
                result.Add(new DimProducto
                {
                    IdProducto = first.IdProducto,
                    NombreProducto = GetBestProductName(entries),
                    Categoria = null,
                    Subcategoria = null
                });
                categoriaPerdida++;
            }

            _logger.LogInformation("MapProductos: Total={Total}, ConCategoría={ConCat}, SinCategoría={SinCat}",
                result.Count, categoriaEncontrada, categoriaPerdida);

            if (categoriaPerdida > 0)
            {
                var sinCategoriaEjemplos = result.Where(p => string.IsNullOrWhiteSpace(p.Categoria))
                                                .Take(10)
                                                .Select(p => p.IdProducto)
                                                .ToList();
                _logger.LogWarning("Productos sin categoría final (ejemplos): {Ejemplos}", string.Join(", ", sinCategoriaEjemplos));
            }

            return result;
        }

        private static string GetBestProductName(List<(string IdProducto, string NombreProducto, string? Categoria, string Fuente)> entries)
        {
            var informativos = entries.Where(e => e.Fuente != "CSV" &&
                                                 !string.IsNullOrWhiteSpace(e.NombreProducto) &&
                                                 e.NombreProducto != "Sin Nombre")
                                      .Select(e => e.NombreProducto)
                                      .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(informativos)) return informativos;

            var cualquierNombre = entries.Where(e => !string.IsNullOrWhiteSpace(e.NombreProducto) && e.NombreProducto != "Sin Nombre")
                                         .Select(e => e.NombreProducto)
                                         .FirstOrDefault();

            return cualquierNombre ?? $"Producto {entries.First().IdProducto}";
        }

        private static int GetSourcePriority(string fuente)
        {
            return fuente switch
            {
                "WEB" => 1,
                "API" => 2,
                "CSV" => 3,
                _ => 99
            };
        }

        private List<DimFuente> MapFuentes(ExtractionResult extractionResult)
        {
            var fuentes = new List<DimFuente>();

            if (extractionResult.EncuestasInternas.Any())
            {
                fuentes.Add(new DimFuente
                {
                    IdFuente = "CSV",
                    NombreFuente = "Encuestas Internas CSV",
                    TipoFuente = "Archivo CSV"
                });
            }

            if (extractionResult.WebReviews.Any())
            {
                fuentes.Add(new DimFuente
                {
                    IdFuente = "SAOC_DB",
                    NombreFuente = "Base de Datos SAOC",
                    TipoFuente = "Base de Datos SQL Server"
                });
            }

            if (extractionResult.SocialComments.Any())
            {
                fuentes.Add(new DimFuente
                {
                    IdFuente = "SOCIAL_API",
                    NombreFuente = "API de Comentarios Sociales",
                    TipoFuente = "API REST"
                });
            }

            return fuentes;
        }

        private List<DimSentimiento> MapSentimientos(ExtractionResult extractionResult)
        {
            var sentimientos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var inferredSocial = extractionResult.SocialComments
                .Select(c => new { Texto = c.Comentario ?? string.Empty, Inferred = AnalizarSentimientoTexto(c.Comentario) })
                .ToList();

            var socialPos = inferredSocial.Count(x => x.Inferred == "Positivo");
            var socialNeg = inferredSocial.Count(x => x.Inferred == "Negativo");
            var socialNeu = inferredSocial.Count(x => x.Inferred == "Neutral");

            _logger.LogInformation("MapSentimientos (social): Positivo={Pos} Negativo={Neg} Neutral={Neu} (total={Total})",
                socialPos, socialNeg, socialNeu, inferredSocial.Count);

            string GetExamples(string category) =>
                string.Join(" || ", inferredSocial.Where(x => x.Inferred == category).Select(x => x.Texto).Take(10));

            _logger.LogDebug("Ejemplos Positivo: {E}", GetExamples("Positivo"));
            _logger.LogDebug("Ejemplos Negativo: {E}", GetExamples("Negativo"));
            _logger.LogDebug("Ejemplos Neutral: {E}", GetExamples("Neutral"));

            foreach (var encuesta in extractionResult.EncuestasInternas)
            {
                var clas = NormalizarSentimiento(encuesta.Clasificacion);
                sentimientos.Add(clas);
            }

            foreach (var review in extractionResult.WebReviews)
            {
                var sentimiento = review.Rating switch
                {
                    >= 4 => "Positivo",
                    <= 2 => "Negativo",
                    _ => "Neutral"
                };
                sentimientos.Add(sentimiento);
            }

            foreach (var inf in inferredSocial)
                sentimientos.Add(inf.Inferred);

            sentimientos.Add("Positivo");
            sentimientos.Add("Neutral");
            sentimientos.Add("Negativo");

            _logger.LogDebug("MapSentimientos - valores normalizados: {Vals}", string.Join(", ", sentimientos.OrderBy(s => s)));

            return sentimientos.Select(s => new DimSentimiento
            {
                Clasificacion = s,
                Descripcion = GetSentimientoDescripcion(s),
                ValorNumerico = GetSentimientoValor(s)
            }).ToList();
        }

        private List<DimTiempo> MapTiempos(ExtractionResult extractionResult)
        {
            var fechas = new HashSet<DateTime>();

            foreach (var encuesta in extractionResult.EncuestasInternas)
                fechas.Add(encuesta.Fecha.Date);

            foreach (var review in extractionResult.WebReviews)
                fechas.Add(review.Fecha.Date);

            foreach (var comment in extractionResult.SocialComments)
                fechas.Add(comment.Fecha.Date);

            return fechas.Select(fecha => new DimTiempo
            {
                Fecha = fecha,
                Anio = fecha.Year,
                Mes = fecha.Month,
                Dia = fecha.Day,
                Trimestre = ((fecha.Month - 1) / 3) + 1,
                MesNombre = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(fecha.Month),
                Semana = ISOWeek.GetWeekOfYear(fecha)
            }).ToList();
        }

        #endregion

        #region Helpers

        private static string GetSentimientoDescripcion(string clasificacion)
        {
            return clasificacion?.ToLower() switch
            {
                "positivo" => "Comentario con connotación positiva",
                "negativo" => "Comentario con connotación negativa",
                "neutral" => "Comentario sin connotación específica",
                _ => "Clasificación no determinada"
            };
        }

        private static int GetSentimientoValor(string clasificacion)
        {
            return clasificacion?.ToLower() switch
            {
                "positivo" => 1,
                "negativo" => -1,
                "neutral" => 0,
                _ => 0
            };
        }

        private static string NormalizarSentimiento(string? clasificacion)
        {
            if (string.IsNullOrWhiteSpace(clasificacion)) return "Neutral";

            var lower = RemoveDiacritics(clasificacion).ToLowerInvariant().Trim();
            return lower switch
            {
                "positivo" or "positiva" or "pos" or "good" => "Positivo",
                "negativo" or "negativa" or "neg" or "bad" => "Negativo",
                "neutral" or "neutro" or "n" => "Neutral",
                _ => "Neutral"
            };
        }

        private static string AnalizarSentimientoTexto(string? comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario)) return "Neutral";

            var texto = RemoveDiacritics(comentario).ToLowerInvariant();

            string[] negativePhrases = {
                "no funciona", "no cumple", "no lo recomiendo", "no recomiendo", "no recomendable",
                "muy mala calidad", "mala calidad", "pesima atencion", "se rompio", "se rompio rapido",
                "no cumple con lo anunciado", "insatisfecho", "decepcionado", "defectuoso", "no funciona como esperaba"
            };

            string[] negativas = {
                "malo","mala","pesimo","pesima","pesimo","pesima","terrible","horrible","deficiente","defectuoso","insatisfecho","decepcionado","no cumple"
            };

            string[] positivas = {
                "excelente","bueno","genial","recomiendo","perfecto","increible","agradable","satisfecho","muy satisfecho","calidad superior",
                "me encanta","lo recomiendo","muy contento","cumple su funcion","llego rapido","funciona perfecto","gran relacion calidad"
            };

            int negScore = 0;
            int posScore = 0;

            foreach (var phrase in negativePhrases)
            {
                if (texto.Contains(phrase)) negScore += 3;
            }

            bool ContainsWord(string t, string word)
            {
                return Regex.IsMatch(t, @"\b" + Regex.Escape(word) + @"\b", RegexOptions.CultureInvariant);
            }

            foreach (var w in negativas)
            {
                if (ContainsWord(texto, w)) negScore++;
            }

            foreach (var w in positivas)
            {
                if (ContainsWord(texto, w)) posScore++;
            }

            if (Regex.IsMatch(texto, @"\bno\b\s+\b(recomiendo|recomendable|recomendado|buen|bueno|excelente|perfecto)\b"))
                negScore += 2;

            if (posScore > negScore) return "Positivo";
            if (negScore > posScore) return "Negativo";
            return "Neutral";
        }

        private static string RemoveDiacritics(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string NormalizeProdId(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim();
            if (s.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1);
            return s.ToUpperInvariant();
        }

        private static bool IsGenericCategory(string? categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return true;
            var lower = categoria.Trim().ToLowerInvariant();
            return lower.Contains("sin") || lower.Contains("desconocido") || lower.Contains("generales") || lower.Contains("otros");
        }

        #endregion
    }
}

