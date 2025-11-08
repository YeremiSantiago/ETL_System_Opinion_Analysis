using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Interfaces
{
    public interface IReaderRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetByConditionAsync(Func<T, bool> predicate);
        Task<int> CountAsync();
        Task<bool> ExistsAsync(object id);
    }
}
