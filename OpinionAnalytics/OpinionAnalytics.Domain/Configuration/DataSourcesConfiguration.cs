using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Configuration
{
    public class DataSourcesConfiguration
    {
        public const string SectionName = "DataSources";

        public string CsvFilePath { get; set; } = string.Empty;
        public string ProductosCsvPath { get; set; } = string.Empty;
        public string ClientesCsvPath { get; set; } = string.Empty;
        
        public ApiSettings ApiSettings { get; set; } = new();
    }
}
