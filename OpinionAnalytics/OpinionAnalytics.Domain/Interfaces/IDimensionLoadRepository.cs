using OpinionAnalytics.Domain.Dtos;

namespace OpinionAnalytics.Domain.Interfaces
{
    public interface IDimensionLoadRepository
    {
        Task<DimensionLoadResult> LoadDimensionsAsync(DimensionLoadDto dto, CancellationToken cancellationToken = default);
    }
}
