using OpinionAnalytics.Domain.Dtos;
using OpinionAnalytics.Domain.Entities.Db;
using OpinionAnalytics.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Interfaces
{
    public interface IWebReviewDbExtractorRepository : IReaderRepository<WebReviewView>
    {
        Task<IEnumerable<WebReviewView>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<WebReviewView>> GetByProductAsync(int productId);
        Task<IEnumerable<WebReviewView>> GetByRatingRangeAsync(int minRating, int maxRating);
        Task<IEnumerable<WebReviewView>> GetForDimensionLoadingAsync(CancellationToken cancellationToken = default);
    }
}
