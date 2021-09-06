using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLibrary.Models
{
    public class BasicCaseModel
    {
        public string Id { get; set; }
        public string FormType { get; set; }
        public DateTime RefreshDateTime { get; set; }

        public BasicCaseModel(System.String id, System.String form, System.String refresh)
        {
            Id = id;
            FormType = form;
            RefreshDateTime = DateTime.Parse(refresh);
        }
        
    }
}
