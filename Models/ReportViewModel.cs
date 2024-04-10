using Microsoft.PowerBI.Api.Models;

namespace CRES.Services.Models
{
    public class ReportViewModel
    {
        public Report Report { get; set; }

        public string AccessToken { get; set; }
    }
}