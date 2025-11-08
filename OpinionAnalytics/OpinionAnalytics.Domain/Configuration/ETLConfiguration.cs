using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Configuration
{
    public class ETLConfiguration
    {
        public const string SectionName = "ETL";

        public int BatchSize { get; set; } = 1000;
        public int ProcessingIntervalMinutes { get; set; } = 60;
        public bool EnableParallelProcessing { get; set; } = true;

    }
}
