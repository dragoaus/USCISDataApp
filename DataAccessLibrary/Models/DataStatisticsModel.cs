using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccessLibrary.Models
{
    public class DataStatisticsModel
    {
        public List<FullCaseModel> AllCases;
        public List<FullCaseModel> UntouchedCases;
        public List<FullCaseModel> OpenCases;
        public List<FullCaseModel> ClosedCases;
        public List<Tuple<DateTime, int>> UntouchedCasesStatistics;
        public List<Tuple<DateTime, int>> OpenCasesStatistics;
        public List<Tuple<DateTime, int>> ClosedCasesStatistics;
        public List<Tuple<string, int, int, int, int>> StatusPerUscisGroup;


        public DataStatisticsModel(List<FullCaseModel>cases)
        {
            AllCases = cases;
            UntouchedCases = cases.Where(c => c.CaseStatus == "Case Was Received" ).ToList();

            OpenCases = cases.Where(c =>
                                                             c.CaseStatus == "Name Was Updated" ||
                                                             c.CaseStatus == "Date of Birth Was Updated" ||
                                                             c.CaseStatus == "Request for Additional Evidence Was Sent" ||
                                                             c.CaseStatus == "Request for Initial Evidence Was Sent" ||
                                                             c.CaseStatus == "Request for Initial and Additional Evidence Was Mailed" ||
                                                             c.CaseStatus == "Request for Additional Information Received" ||
                                                             c.CaseStatus == "Response To USCIS&#039; Request For Evidence Was Received" ||
                                                             c.CaseStatus == "Case Transferred And New Office Has Jurisdiction" ||
                                                             c.CaseStatus == "Case Was Transferred And A New Office Has Jurisdiction" ||
                                                             c.CaseStatus == "Case Was Relocated From Administrative Appeals Office To USCIS Originating Office" ||
                                                             c.CaseStatus == "Department of State Sent Case to USCIS For Review" ||
                                                             c.CaseStatus == "Expedite Request Approved" ||
                                                             c.CaseStatus == "Expedite Request Denied" ||
                                                             c.CaseStatus == "Document Was Mailed" || 
                                                             c.CaseStatus == "Case Was Reopened").ToList();

            ClosedCases = cases.Where(c => c.CaseStatus == "Case Was Approved" ||
                                                             c.CaseStatus == "Case Was Denied" ||
                                                             c.CaseStatus == "Case Closed Benefit Received By Other Means" ||
                                                             c.CaseStatus == "Case Rejected Because I Sent An Incorrect Fee" ||
                                                             c.CaseStatus == "Case Was Rejected Because I Did Not Sign My Form" ||
                                                             c.CaseStatus == "Case Was Rejected Because It Was Improperly Filed" ||
                                                             c.CaseStatus == "Notice Explaining USCIS Actions Was Mailed" ||
                                                             c.CaseStatus == "Document Was Returned To USCIS" ||
                                                             c.CaseStatus == "Document Is Being Held For 180 Days" ||
                                                             c.CaseStatus == "Termination Notice Sent" ||
                                                             c.CaseStatus == "Withdrawal Acknowledgement Notice Was Sent").ToList();



            StatusPerUscisGroup = new List<Tuple<string, int, int, int, int>>();
            //Group first 9 characters WAC2190093072 => WAC2190093xxx
            var casesGroupedByUscisNum = AllCases.GroupBy(c => new string(c.Id.Substring(0, 10)));
            casesGroupedByUscisNum = casesGroupedByUscisNum.OrderBy(c => c.Key);

            var untouchedCasesGroupedByUscisNum = UntouchedCases.GroupBy(c => new string(c.Id.Substring(0, 10)));
            var openCasesGroupedByUscisNum = OpenCases.GroupBy(c => new string(c.Id.Substring(0, 10)));
            var closedCasesGroupedByUscisNum = ClosedCases.GroupBy(c => new string(c.Id.Substring(0, 10)));

            foreach (var c in casesGroupedByUscisNum)
            {
                var untouchedCaseStats = untouchedCasesGroupedByUscisNum.Where(oc => oc.Key == c.Key).FirstOrDefault();
                var openCaseStats = openCasesGroupedByUscisNum.Where(oc => oc.Key == c.Key).FirstOrDefault();
                var closedCaseStats = closedCasesGroupedByUscisNum.Where(oc => oc.Key == c.Key).FirstOrDefault();

                var caseNumGroup = c.Key;
                var totalCasesInGroup = c.Count();
                var untouchedCasesInGroup = untouchedCaseStats?.Count() ?? 0;
                var openCasesInGroup = openCaseStats?.Count() ?? 0;
                var closedCasesInGroup = closedCaseStats?.Count() ?? 0;

                StatusPerUscisGroup.Add(new Tuple<string, int, int, int, int>(caseNumGroup, totalCasesInGroup, untouchedCasesInGroup, openCasesInGroup, closedCasesInGroup));
            }

            var groupedCases = UntouchedCases.GroupBy(c => new { c.LastStatusChange.Year, c.LastStatusChange.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            UntouchedCasesStatistics = new List<Tuple<DateTime, int>>();
            foreach (var c in groupedCases)
            {
                UntouchedCasesStatistics.Add(new Tuple<DateTime, int>(new DateTime(c.Key.Year, c.Key.Month, 15), c.Count()));
            }

            groupedCases = OpenCases.GroupBy(c => new { c.LastStatusChange.Year, c.LastStatusChange.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            OpenCasesStatistics = new List<Tuple<DateTime, int>>();
            foreach (var c in groupedCases)
            {
                OpenCasesStatistics.Add(new Tuple<DateTime, int>(new DateTime(c.Key.Year, c.Key.Month, 15), c.Count()));
            }

            groupedCases = ClosedCases.GroupBy(c => new { c.LastStatusChange.Year, c.LastStatusChange.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            ClosedCasesStatistics = new List<Tuple<DateTime, int>>();
            foreach (var c in groupedCases)
            {
                ClosedCasesStatistics.Add(new Tuple<DateTime, int>(new DateTime(c.Key.Year, c.Key.Month, 15), c.Count()));
            }

        }
        /// <summary>
        /// Gets basis stats from cases based on case status (untouched, open, closed)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public string GetStatsForCase(Tuple<DateTime,int> c)
        {
            return $"Date: {c.Item1.Year:D4}.{c.Item1.Month:D2} - Cases: {c.Item2:D5} ";
        }
        public string GetStatsForCaseGroup(Tuple<string, int, int, int, int> c)
        {
            return $"Case Group: {c.Item1} - Total Cases:{c.Item2:D5} - Untouched: {c.Item3:D5} - Open: {c.Item4:D5} - Closed: {c.Item5:D5}";
        }

        public string GetBasicStatisticsForFullCases(List<Tuple<DateTime, int>> cases, string message)
        {
            string output = "";
            
            output += Environment.NewLine;
            output += message;

            foreach (var caseStats in cases)
            {
                output += Environment.NewLine;
                output += GetStatsForCase(caseStats).ToString();
            }
            output += Environment.NewLine;
            output += Environment.NewLine;
            return output;
        }

        public string GetBasicStatisticsForUscisGroups(List<Tuple<string, int, int, int, int>>  cases, string message)
        {
            string output = "";

            output += Environment.NewLine;
            output += message;

            foreach (var caseStats in cases)
            {
                output += Environment.NewLine;
                output += GetStatsForCaseGroup(caseStats).ToString();
            }
            output += Environment.NewLine;
            output += Environment.NewLine;


            return output;
        }

    }
}
