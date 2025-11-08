using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Api;
using OpinionAnalytics.Domain.Entities.Csv;
using OpinionAnalytics.Domain.Entities.Db;

namespace OpinionAnalytics.Application.Interfaces;

public interface IDataExtractionService
{
    Task<IEnumerable<EncuestaInterna>> ExtractCsvDataAsync();
    Task<IEnumerable<EncuestaInterna>> ExtractCsvDataAsync(string csvFilePath);
    Task<IEnumerable<WebReviewView>> ExtractDatabaseDataAsync();
    Task<IEnumerable<SocialCommentView>> ExtractApiDataAsync();
    
    Task<IEnumerable<EncuestaInterna>> ExtractCsvByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<EncuestaInterna>> ExtractCsvByDateRangeAsync(string csvFilePath, DateTime startDate, DateTime endDate);
    Task<IEnumerable<WebReviewView>> ExtractDatabaseByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<SocialCommentView>> ExtractApiByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<ExtractionResult> ExtractAllDataParallelAsync();
    Task<ExtractionMetrics> GetExtractionMetricsAsync();
    Task RefreshApiCacheAsync();
    bool ValidateDataSourcesConfiguration();
}
