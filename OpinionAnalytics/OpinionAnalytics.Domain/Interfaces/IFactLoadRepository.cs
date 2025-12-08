using System.Threading;
using System.Threading.Tasks;
using OpinionAnalytics.Domain.Dtos;

namespace OpinionAnalytics.Domain.Interfaces
{
    public interface IFactLoadRepository
    {
        Task CleanFactTableAsync(CancellationToken cancellationToken = default);

        Task<int> LoadFactsAsync(ExtractionResult extractionResult, CancellationToken cancellationToken = default);
    }
}
