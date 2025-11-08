using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Dtos
{
    public class ExtractionMetrics
    {
        public int CsvRecordsCount { get; set; }
        public int DatabaseRecordsCount { get; set; }
        public int ApiRecordsCount { get; set; }
        public int TotalRecordsExtracted => CsvRecordsCount + DatabaseRecordsCount + ApiRecordsCount;
        
        public TimeSpan CsvExtractionTime { get; set; }
        public TimeSpan DatabaseExtractionTime { get; set; }
        public TimeSpan ApiExtractionTime { get; set; }
        public TimeSpan TotalExtractionTime { get; set; }  
        
        public List<string> ExtractedSources { get; set; } = new();
        public DateTime LastExtractionTime { get; set; } = DateTime.UtcNow;
        
        public double RecordsPerSecond => TotalExtractionTime.TotalSeconds > 0 
            ? TotalRecordsExtracted / TotalExtractionTime.TotalSeconds 
            : 0;
            
        public int ApiCallsCount { get; set; } = 1;  
    }
}
