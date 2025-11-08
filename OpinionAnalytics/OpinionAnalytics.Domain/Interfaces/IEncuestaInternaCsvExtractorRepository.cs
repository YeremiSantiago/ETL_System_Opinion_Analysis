using OpinionAnalytics.Domain.Entities.Csv;
using OpinionAnalytics.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Persistence.Repositories.Csv
{
    public interface IEncuestaInternaCsvExtractorRepository : IReaderRepository<EncuestaInterna>
    {
        Task<IEnumerable<EncuestaInterna>> LoadFromCsvAsync(string filePath);
        Task<IEnumerable<EncuestaInterna>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<EncuestaInterna>> GetByClasificacionAsync(string clasificacion);
        Task<IEnumerable<EncuestaInterna>> GetByProductAsync(int productId);

    }
}
