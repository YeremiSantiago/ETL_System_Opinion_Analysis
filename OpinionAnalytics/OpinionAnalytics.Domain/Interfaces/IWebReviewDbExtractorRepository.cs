using OpinionAnalytics.Domain.Entities.Db;
using OpinionAnalytics.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Db
{
    public interface IWebReviewDbExtractorRepository : IReaderRepository<WebReviewView>
    {
        Task<IEnumerable<WebReviewView>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<WebReviewView>> GetByProductAsync(int productId);
        Task<IEnumerable<WebReviewView>> GetByRatingRangeAsync(int minRating, int maxRating);

    }
}
