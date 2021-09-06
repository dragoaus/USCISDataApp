using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLibrary.Models
{
    public class CaseStatusModel
    {
        public string Id { get; set; }
        public string CaseStatus { get; set; }
        public DateTime LastUpDateTime { get; set; }
        public string CaseInfos { get; set; }
    }
}
