using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using DataAccessLibrary.Annotations;

namespace DataAccessLibrary.Models
{
    public class FullCaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Id { get; set; }
        public string CaseStatus { get; set; }
        public DateTime LastUpDateTime { get; set; }
        public string FormType { get; set; }
        public DateTime RefreshDateTime { get; set; }
        public string CaseInfos { get; set; }

        public FullCaseModel(System.String id, System.String status, System.String lastUpdate, System.String form, System.String refresh, System.String caseDetails)
        {
            Id = id;
            CaseStatus = status;
            LastUpDateTime = DateTime.Parse(lastUpdate);
            FormType = form;
            RefreshDateTime = DateTime.Parse(refresh);
            CaseInfos = caseDetails;
        }

        public FullCaseModel(Tuple<string, string, DateTime, string, DateTime, string> caseInformation)
        {
            Id = caseInformation.Item1;
            CaseStatus= caseInformation.Item2;
            LastUpDateTime = caseInformation.Item3;
            FormType = caseInformation.Item4;
            RefreshDateTime = caseInformation.Item5;
            CaseInfos = caseInformation.Item6;
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}
