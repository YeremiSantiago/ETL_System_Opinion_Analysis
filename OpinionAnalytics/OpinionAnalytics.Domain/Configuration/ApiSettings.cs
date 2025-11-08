using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpinionAnalytics.Domain.Configuration
{
    public class ApiSettings
    {
        public string SocialCommentsUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;

    }
}
