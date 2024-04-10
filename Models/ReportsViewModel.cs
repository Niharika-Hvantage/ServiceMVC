using Microsoft.PowerBI.Api.Models;
using System.Collections.Generic;

namespace CRES.Services.Models
{
    public class ReportsViewModel
    {
        public List<Report> Reports { get; set; }
        public List<Microsoft.PowerBI.Api.Models.Report> pbiReports { get; set; }
    }
}