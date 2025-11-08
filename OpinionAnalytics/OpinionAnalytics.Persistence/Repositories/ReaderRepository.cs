using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories
{
    public class ReaderRepository<T> : IReaderRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public ReaderRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetByConditionAsync(Func<T, bool> predicate)
        {
            return await Task.FromResult(_dbSet.AsNoTracking().Where(predicate));
        }

        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public virtual async Task<bool> ExistsAsync(object id)
        {
            var entity = await GetByIdAsync(id);
            return entity != null;
        }
    }
}
