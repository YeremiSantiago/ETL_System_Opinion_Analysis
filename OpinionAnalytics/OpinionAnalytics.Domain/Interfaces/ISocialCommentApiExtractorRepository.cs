using OpinionAnalytics.Domain.Entities.Api;
using OpinionAnalytics.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Api
{
    public interface ISocialCommentApiExtractorRepository : IReaderRepository<SocialCommentView>
    {
        Task<IEnumerable<SocialCommentView>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<SocialCommentView>> GetBySourceAsync(string source);
        Task<IEnumerable<SocialCommentView>> GetByProductAsync(int productId);
        Task RefreshCacheAsync();
    }
}
