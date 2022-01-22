using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;


namespace DataAccessLibrary.Models
{
    public class FullCaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Id { get; set; }
        public string CaseStatus { get; set; }
        public DateTime LastStatusChange { get; set; }
        public string FormType { get; set; }
        public DateTime RefreshDate { get; set; }
        public string CaseInfos { get; set; }

        public FullCaseModel(System.String id, System.String status, System.String lastUpdate, System.String form, System.String refresh, System.String caseDetails)
        {
            Id = id;
            CaseStatus = status;
            LastStatusChange = DateTime.Parse(lastUpdate);
            FormType = form;
            RefreshDate = DateTime.Parse(refresh);
            CaseInfos = caseDetails;
        }

        public FullCaseModel(Tuple<string, string, DateTime, string, DateTime, string> caseInformation)
        {
            Id = caseInformation.Item1;
            CaseStatus = caseInformation.Item2;
            LastStatusChange = caseInformation.Item3;
            FormType = caseInformation.Item4;
            RefreshDate = caseInformation.Item5;
            CaseInfos = caseInformation.Item6;
        }
    }
}
