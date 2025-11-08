using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Api.Data.Context;
using OpinionAnalytics.Domain.Entities.Api;

namespace OpinionAnalytics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SocialCommentsController : ControllerBase
{
    private readonly ApiDbContext _context;
    private readonly ILogger<SocialCommentsController> _logger;

    public SocialCommentsController(ApiDbContext context, ILogger<SocialCommentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los comentarios de redes sociales
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SocialCommentView>>> GetSocialComments()
    {
        try
        {
            _logger.LogInformation("Obteniendo todos los comentarios de redes sociales");
            
            var comments = await _context.SocialCommentsView
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} comentarios", comments.Count);
            
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo comentarios de redes sociales");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtiene comentarios por fuente específica (Instagram, Twitter, Facebook)
    /// </summary>
    [HttpGet("by-source/{source}")]
    public async Task<ActionResult<IEnumerable<SocialCommentView>>> GetSocialCommentsBySource(string source)
    {
        try
        {
            _logger.LogInformation("Obteniendo comentarios de la fuente: {Source}", source);
            
            var comments = await _context.SocialCommentsView
                .Where(s => s.Fuente.Contains(source))
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} comentarios de {Source}", comments.Count, source);
            
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo comentarios por fuente");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtiene comentarios por producto específico
    /// </summary>
    [HttpGet("by-product/{productId}")]
    public async Task<ActionResult<IEnumerable<SocialCommentView>>> GetSocialCommentsByProduct(int productId)
    {
        try
        {
            _logger.LogInformation("Obteniendo comentarios del producto: {ProductId}", productId);
            
            var comments = await _context.SocialCommentsView
                .Where(s => s.IdProducto == productId)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} comentarios del producto {ProductId}", comments.Count, productId);
            
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo comentarios por producto");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtiene un comentario específico por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SocialCommentView>> GetSocialComment(int id)
    {
        try
        {
            _logger.LogInformation("Obteniendo comentario con ID: {Id}", id);
            
            var comment = await _context.SocialCommentsView
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdComment == id);

            if (comment == null)
            {
                _logger.LogWarning("Comentario con ID {Id} no encontrado", id);
                return NotFound($"Comentario con ID {id} no encontrado");
            }

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo comentario por ID");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtiene métricas de comentarios
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<object>> GetSocialCommentsMetrics()
    {
        try
        {
            _logger.LogInformation("Obteniendo métricas de comentarios");
            
            var totalComments = await _context.SocialCommentsView.CountAsync();
            var commentsBySource = await _context.SocialCommentsView
                .GroupBy(s => s.Fuente)
                .Select(g => new { Fuente = g.Key, Count = g.Count() })
                .ToListAsync();

            var latestComment = await _context.SocialCommentsView
                .OrderByDescending(s => s.Fecha)
                .FirstOrDefaultAsync();

            var metrics = new
            {
                TotalComments = totalComments,
                CommentsBySource = commentsBySource,
                LatestCommentDate = latestComment?.Fecha,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Métricas obtenidas: {TotalComments} comentarios totales", totalComments);
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo métricas");
            return StatusCode(500, "Error interno del servidor");
        }
    }
}
