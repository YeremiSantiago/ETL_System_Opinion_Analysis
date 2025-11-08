using CsvHelper;
using OpinionAnalytics.Domain.Entities.Csv;
using System.Globalization;

namespace OpinionAnalytics.Persistence.Repositories.Csv
{
    public class EncuestaInternaCsvExtractorRepository : IEncuestaInternaCsvExtractorRepository
    {
        private readonly List<EncuestaInterna> _encuestas = new();

        public async Task<IEnumerable<EncuestaInterna>> GetAllAsync()
        {
            return await Task.FromResult(_encuestas.AsEnumerable());
        }

        public async Task<EncuestaInterna?> GetByIdAsync(object id)
        {
            return await Task.FromResult(_encuestas.FirstOrDefault(e => e.IdOpinion == (int)id));
        }

        public async Task<IEnumerable<EncuestaInterna>> GetByConditionAsync(Func<EncuestaInterna, bool> predicate)
        {
            return await Task.FromResult(_encuestas.Where(predicate));
        }

        public async Task<int> CountAsync()
        {
            return await Task.FromResult(_encuestas.Count);
        }

        public async Task<bool> ExistsAsync(object id)
        {
            return await Task.FromResult(_encuestas.Any(e => e.IdOpinion == (int)id));
        }


        public async Task<IEnumerable<EncuestaInterna>> LoadFromCsvAsync(string filePath)
        {
            _encuestas.Clear();

            using var reader = new StringReader(await File.ReadAllTextAsync(filePath));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<EncuestaInterna>().ToList();
            _encuestas.AddRange(records);

            return _encuestas;
        }

        public async Task<IEnumerable<EncuestaInterna>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetByConditionAsync(e => e.Fecha >= startDate && e.Fecha <= endDate);
        }

        public async Task<IEnumerable<EncuestaInterna>> GetByClasificacionAsync(string clasificacion)
        {
            return await GetByConditionAsync(e => e.Clasificacion == clasificacion);
        }

        public async Task<IEnumerable<EncuestaInterna>> GetByProductAsync(int productId)
        {
            return await GetByConditionAsync(e => e.IdProducto == productId);
        }
    }
}

