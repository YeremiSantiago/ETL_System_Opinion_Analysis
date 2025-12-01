using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Dwh.Dimensions;
using OpinionAnalytics.Domain.Interfaces;
using OpinionAnalytics.Persistence.Repositories.Dwh.Context;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Dwh
{
    public class DimensionLoadRepository : IDimensionLoadRepository
    {
       
        private readonly DwhDbContext _context;
        private readonly ILogger<DimensionLoadRepository> _logger;

        public DimensionLoadRepository(DwhDbContext context, ILogger<DimensionLoadRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DimensionLoadResult> LoadDimensionsAsync(DimensionLoadDto dto, CancellationToken cancellationToken = default)
        {
            var result = new DimensionLoadResult();

            if (dto == null)
            {
                result.Errors.Add("DimensionLoadDto es null");
                return result;
            }

            NormalizeDto(dto, _logger);

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var clientesDict = await _context.DimClientes.AsNoTracking()
                    .ToDictionaryAsync(c => c.IdCliente, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var productosDict = await _context.DimProductos.AsNoTracking()
                    .ToDictionaryAsync(p => p.IdProducto, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var fuentesDict = await _context.DimFuentes.AsNoTracking()
                    .ToDictionaryAsync(f => f.IdFuente, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var sentimientosDict = await _context.DimSentimientos.AsNoTracking()
                    .ToDictionaryAsync(s => s.Clasificacion, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var tiemposDict = await _context.DimTiempos.AsNoTracking()
                    .ToDictionaryAsync(t => t.Fecha.Date, cancellationToken);

               
                var clientesExistentes = dto.Clientes
                    .Where(c => !string.IsNullOrWhiteSpace(c.IdCliente) && clientesDict.ContainsKey(c.IdCliente))
                    .Select(c =>
                    {
                        var existing = clientesDict[c.IdCliente];
                        if (!string.Equals(existing.Nombre, c.Nombre, StringComparison.Ordinal) ||
                            !string.Equals(existing.Email, c.Email, StringComparison.Ordinal))
                        {
                            existing.Nombre = c.Nombre;
                            existing.Email = c.Email;
                            return existing;
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .Cast<DimCliente>()
                    .ToList();

                var clientesNuevos = dto.Clientes
                    .Where(c => !string.IsNullOrWhiteSpace(c.IdCliente) && !clientesDict.ContainsKey(c.IdCliente))
                    .ToList();

                result.ClientesUpdated = clientesExistentes.Count;
                result.ClientesInserted = clientesNuevos.Count;

                
                var productosExistentes = dto.Productos
                    .Where(p => !string.IsNullOrWhiteSpace(p.IdProducto) && productosDict.ContainsKey(p.IdProducto))
                    .Select(p =>
                    {
                        var existing = productosDict[p.IdProducto];
                        if (!string.Equals(existing.NombreProducto, p.NombreProducto, StringComparison.Ordinal) ||
                            !string.Equals(existing.Categoria ?? string.Empty, p.Categoria ?? string.Empty, StringComparison.Ordinal) ||
                            !string.Equals(existing.Subcategoria ?? string.Empty, p.Subcategoria ?? string.Empty, StringComparison.Ordinal))
                        {
                            existing.NombreProducto = p.NombreProducto;
                            existing.Categoria = p.Categoria;
                            existing.Subcategoria = p.Subcategoria;
                            return existing;
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .Cast<DimProducto>()
                    .ToList();

                var productosNuevos = dto.Productos
                    .Where(p => !string.IsNullOrWhiteSpace(p.IdProducto) && !productosDict.ContainsKey(p.IdProducto))
                    .ToList();

                result.ProductosUpdated = productosExistentes.Count;
                result.ProductosInserted = productosNuevos.Count;

                
                var fuentesExistentes = dto.Fuentes
                    .Where(f => !string.IsNullOrWhiteSpace(f.IdFuente) && fuentesDict.ContainsKey(f.IdFuente))
                    .Select(f =>
                    {
                        var existing = fuentesDict[f.IdFuente];
                        if (!string.Equals(existing.NombreFuente, f.NombreFuente, StringComparison.Ordinal) ||
                            !string.Equals(existing.TipoFuente, f.TipoFuente, StringComparison.Ordinal))
                        {
                            existing.NombreFuente = f.NombreFuente;
                            existing.TipoFuente = f.TipoFuente;
                            existing.FechaCarga = DateTime.UtcNow;
                            return existing;
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .Cast<DimFuente>()
                    .ToList();

                var fuentesNuevas = dto.Fuentes
                    .Where(f => !string.IsNullOrWhiteSpace(f.IdFuente) && !fuentesDict.ContainsKey(f.IdFuente))
                    .Select(f =>
                    {
                        f.FechaCarga = DateTime.UtcNow;
                        return f;
                    })
                    .ToList();

                result.FuentesUpdated = fuentesExistentes.Count;
                result.FuentesInserted = fuentesNuevas.Count;

              
                var sentimientosExistentes = dto.Sentimientos
                    .Where(s => !string.IsNullOrWhiteSpace(s.Clasificacion) && sentimientosDict.ContainsKey(s.Clasificacion))
                    .Select(s =>
                    {
                        var existing = sentimientosDict[s.Clasificacion];
                        if (!string.Equals(existing.Descripcion ?? string.Empty, s.Descripcion ?? string.Empty, StringComparison.Ordinal) ||
                            existing.ValorNumerico != s.ValorNumerico)
                        {
                            existing.Descripcion = s.Descripcion;
                            existing.ValorNumerico = s.ValorNumerico;
                            return existing;
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .Cast<DimSentimiento>()
                    .ToList();

                var sentimientosNuevos = dto.Sentimientos
                    .Where(s => !string.IsNullOrWhiteSpace(s.Clasificacion) && !sentimientosDict.ContainsKey(s.Clasificacion))
                    .ToList();

                result.SentimientosUpdated = sentimientosExistentes.Count;
                result.SentimientosInserted = sentimientosNuevos.Count;

                
                var tiemposExistentes = dto.Tiempos
                    .Select(t =>
                    {
                        var date = t.Fecha.Date;
                        if (tiemposDict.TryGetValue(date, out var existing))
                        {
                            if (existing.Anio != t.Anio || existing.Mes != t.Mes || existing.Dia != t.Dia)
                            {
                                existing.Anio = t.Anio;
                                existing.Mes = t.Mes;
                                existing.Dia = t.Dia;
                                existing.Trimestre = t.Trimestre;
                                existing.Semana = t.Semana;
                                existing.MesNombre = t.MesNombre;
                                return existing;
                            }
                            return null;
                        }
                        else
                        {
                            t.TiempoKey = ComputeTiempoKey(date);
                            return t;
                        }
                    })
                    .Where(x => x != null)
                    .Cast<DimTiempo>()
                    .ToList();

                var tiemposNuevos = tiemposExistentes.Where(t => t.TiempoKey > 0 && !tiemposDict.ContainsKey(t.Fecha.Date)).ToList();
                var tiemposActualizar = tiemposExistentes.Where(t => tiemposDict.ContainsKey(t.Fecha.Date)).ToList();

                result.TiemposInserted = tiemposNuevos.Count;
                result.TiemposUpdated = tiemposActualizar.Count;

                
                if (clientesNuevos.Any())
                    await _context.DimClientes.AddRangeAsync(clientesNuevos, cancellationToken);
                if (clientesExistentes.Any())
                    _context.DimClientes.UpdateRange(clientesExistentes);

                if (productosNuevos.Any())
                    await _context.DimProductos.AddRangeAsync(productosNuevos, cancellationToken);
                if (productosExistentes.Any())
                    _context.DimProductos.UpdateRange(productosExistentes);

                if (fuentesNuevas.Any())
                    await _context.DimFuentes.AddRangeAsync(fuentesNuevas, cancellationToken);
                if (fuentesExistentes.Any())
                    _context.DimFuentes.UpdateRange(fuentesExistentes);

                if (sentimientosNuevos.Any())
                    await _context.DimSentimientos.AddRangeAsync(sentimientosNuevos, cancellationToken);
                if (sentimientosExistentes.Any())
                    _context.DimSentimientos.UpdateRange(sentimientosExistentes);

                if (tiemposNuevos.Any())
                    await _context.DimTiempos.AddRangeAsync(tiemposNuevos, cancellationToken);
                if (tiemposActualizar.Any())
                    _context.DimTiempos.UpdateRange(tiemposActualizar);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Dimensiones cargadas: Clientes +{Inserted}/{Updated} Productos +{Inserted}/{Updated} Fuentes +{Inserted}/{Updated} Sentimientos +{Inserted}/{Updated} Tiempos +{Inserted}/{Updated}",
                    result.ClientesInserted, result.ClientesUpdated,
                    result.ProductosInserted, result.ProductosUpdated,
                    result.FuentesInserted, result.FuentesUpdated,
                    result.SentimientosInserted, result.SentimientosUpdated,
                    result.TiemposInserted, result.TiemposUpdated);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando dimensiones");
                try { await transaction.RollbackAsync(cancellationToken); } catch { }
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        #region Helpers

        private static void NormalizeDto(DimensionLoadDto dto, ILogger logger)
        {
            foreach (var c in dto.Clientes)
            {
                c.IdCliente = NormalizeId(c.IdCliente);
                c.Nombre = NormalizeString(c.Nombre);
                c.Email = NormalizeOptionalString(c.Email, "Sin Email");
            }

            foreach (var p in dto.Productos)
            {
                p.IdProducto = NormalizeId(p.IdProducto);
                p.NombreProducto = NormalizeString(p.NombreProducto);
                p.Categoria = NormalizeOptionalString(p.Categoria, "Sin Categoría");
                p.Subcategoria = NormalizeOptionalString(p.Subcategoria, "Sin Subcategoría");
            }

            foreach (var f in dto.Fuentes)
            {
                f.IdFuente = NormalizeId(f.IdFuente);
                f.NombreFuente = NormalizeOptionalString(f.NombreFuente, "Sin Nombre");
                f.TipoFuente = NormalizeOptionalString(f.TipoFuente, "Sin Fuente");
            }

            foreach (var s in dto.Sentimientos)
            {
                s.Clasificacion = NormalizeString(s.Clasificacion);
                s.Descripcion = NormalizeOptionalString(s.Descripcion, "Clasificación no determinada");
            }

            for (int i = 0; i < dto.Tiempos.Count; i++)
            {
                var t = dto.Tiempos[i];
                var date = t.Fecha.Date;
                t.Fecha = date;
                t.Anio = date.Year;
                t.Mes = date.Month;
                t.Dia = date.Day;
                t.Trimestre = ((date.Month - 1) / 3) + 1;
                t.MesNombre = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(date.Month);
                t.Semana = System.Globalization.ISOWeek.GetWeekOfYear(date);
            }

           
            dto.Clientes = dto.Clientes
                .GroupBy(x => x.IdCliente, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(c => string.IsNullOrWhiteSpace(c.Email) ? 1 : 0).First())
                .ToList();

            
            dto.Productos = dto.Productos
                .GroupBy(x => x.IdProducto, StringComparer.OrdinalIgnoreCase)
                .Select(g => {
                    var productos = g.ToList();
                    
                    var conCategoriaReal = productos.FirstOrDefault(p => 
                        !string.IsNullOrWhiteSpace(p.Categoria) && 
                        p.Categoria != "Sin Categoría" &&
                        !p.Categoria.ToLower().Contains("sin"));
            
                    if (conCategoriaReal != null) return conCategoriaReal;
            
                    
                    var conCategoria = productos.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Categoria));
            
                    return conCategoria ?? productos.First();
                })
                .ToList();

            dto.Fuentes = dto.Fuentes
                .GroupBy(x => x.IdFuente, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            dto.Sentimientos = dto.Sentimientos
                .GroupBy(x => x.Clasificacion, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            dto.Tiempos = dto.Tiempos
                .GroupBy(x => x.Fecha.Date)
                .Select(g => g.First())
                .ToList();

            var productosConCategoria = dto.Productos.Count(p => !string.IsNullOrWhiteSpace(p.Categoria) && p.Categoria != "Sin Categoría");
            logger.LogInformation("Después de NormalizeDto: {Total} productos, {ConCategoria} con categoría válida", 
                dto.Productos.Count, productosConCategoria);

            if (productosConCategoria < 100) 
            {
                var ejemplos = dto.Productos.Where(p => !string.IsNullOrWhiteSpace(p.Categoria) && p.Categoria != "Sin Categoría")
                                            .Take(10)
                                            .Select(p => $"{p.IdProducto}:{p.Categoria}")
                                            .ToList();
                logger.LogWarning("Productos con categoría después de normalizar (ejemplos): {Ejemplos}", 
                    string.Join(", ", ejemplos));
            }
        }

        private static string NormalizeString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var trimmed = input.Trim();
           
            var normalized = trimmed.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            var result = sb.ToString().Normalize(NormalizationForm.FormC);
            return result;
        }

       
        private static string NormalizeOptionalString(string? input, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            return NormalizeString(input);
        }

        private static string NormalizeId(string? input)
        {
            var s = NormalizeString(input);
            return s.ToUpperInvariant();
        }

        private static int ComputeTiempoKey(DateTime date)
        {
            return date.Year * 10000 + date.Month * 100 + date.Day;
        }

        #endregion
    }
}
