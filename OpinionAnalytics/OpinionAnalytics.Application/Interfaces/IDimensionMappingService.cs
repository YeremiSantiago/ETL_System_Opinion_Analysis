using OpinionAnalytics.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Application.Interfaces
{
    public interface IDimensionMappingService
    {
        DimensionLoadDto MapFromExtractionResult(ExtractionResult extractionResult);
    }
}
