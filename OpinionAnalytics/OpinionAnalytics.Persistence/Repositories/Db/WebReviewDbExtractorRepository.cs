using Microsoft.EntityFrameworkCore;
using OpinionAnalytics.Domain.Entities.Db;
using OpinionAnalytics.Domain.Interfaces;
using OpinionAnalytics.Persistence.Repositories.Db.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Db
{
    public class WebReviewDbExtractorRepository : ReaderRepository<WebReviewView>, IWebReviewDbExtractorRepository
    {
        private readonly ApplicationDbContext _context;

        public WebReviewDbExtractorRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WebReviewView>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(w => w.Fecha >= startDate && w.Fecha <= endDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<WebReviewView>> GetByProductAsync(int productId)
        {
            return await _dbSet
                .Where(w => w.IdProducto == productId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<WebReviewView>> GetByRatingRangeAsync(int minRating, int maxRating)
        {
            return await _dbSet
                .Where(w => w.Rating >= minRating && w.Rating <= maxRating)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<WebReviewView>> GetForDimensionLoadingAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
