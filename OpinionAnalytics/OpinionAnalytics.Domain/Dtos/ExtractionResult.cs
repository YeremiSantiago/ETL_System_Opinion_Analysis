using System;
using System.Collections.Generic;
using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Api;
using OpinionAnalytics.Domain.Entities.Csv;
using OpinionAnalytics.Domain.Entities.Db;

namespace OpinionAnalytics.Domain.Dtos;

public class ExtractionResult
{
    public IEnumerable<EncuestaInterna> EncuestasInternas { get; set; } = new List<EncuestaInterna>();
    public IEnumerable<WebReviewView> WebReviews { get; set; } = new List<WebReviewView>();
    public IEnumerable<SocialCommentView> SocialComments { get; set; } = new List<SocialCommentView>();
    
 
    public IEnumerable<ProductoCsv> ProductosMaestros { get; set; } = new List<ProductoCsv>();
    public IEnumerable<ClienteCsv> ClientesMaestros { get; set; } = new List<ClienteCsv>();
    
    public DateTime ExtractionTimestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Success";
    public List<string> Errors { get; set; } = new();
    public ExtractionMetrics Metrics { get; set; } = new();
}
