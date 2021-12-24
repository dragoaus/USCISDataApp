using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccessLibrary.Models
{
    public class DataStatsModel
    {
        public List<FullCaseModel> AllCases;
        public List<FullCaseModel> UntouchedCases;
        public List<FullCaseModel> OpenCases;
        public List<FullCaseModel> ClosedCases;
        public List<Tuple<int, int, int>> UntouchedCasesStatistics;
        public List<Tuple<int, int, int>> OpenCasesStatistics;
        public List<Tuple<int, int, int>> ClosedCasesStatistics;
        public List<Tuple<string, int, int, int, int>> StatusPerUscisGroup;
        

        public DataStatsModel(List<FullCaseModel>cases)
        {
            AllCases = cases;
            UntouchedCases = cases.Where(c => c.CaseStatus == "Case Was Received").ToList();

            OpenCases = cases.Where(c => c.CaseStatus == "Name Was Updated" ||
                                                             c.CaseStatus == "Request for Additi onal Evidence Was Sent" ||
                                                             c.CaseStatus == "Request for Initial Evidence Was Sent" ||
                                                             c.CaseStatus == "Response To USCIS&#039; Request For Evidence Was Received").ToList();

            ClosedCases = cases.Where(c => c.CaseStatus == "Case Was Approved" ||
                                                               c.CaseStatus == "Case Was Denied" ||
                                                               c.CaseStatus == "Case Closed Benefit Received By Other Means" ||
                                                               c.CaseStatus == "Case Rejected Because I Sent An Incorrect Fee" ||
                                                               c.CaseStatus == "Case Was Rejected Because I Did Not Sign My Form" ||
                                                               c.CaseStatus == "Case Was Rejected Because It Was Improperly Filed" ||
                                                               c.CaseStatus == "Notice Explaining USCIS Actions Was Mailed" ||
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

            var groupedCases = UntouchedCases.GroupBy(c => new { c.LastUpDateTime.Year, c.LastUpDateTime.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            UntouchedCasesStatistics = new List<Tuple<int, int, int>>();
            foreach (var c in groupedCases)
            {
                UntouchedCasesStatistics.Add(new Tuple<int, int, int>(c.Key.Year, c.Key.Month, c.Count()));
            }

            groupedCases = OpenCases.GroupBy(c => new { c.LastUpDateTime.Year, c.LastUpDateTime.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            OpenCasesStatistics = new List<Tuple<int, int, int>>();
            foreach (var c in groupedCases)
            {
                OpenCasesStatistics.Add(new Tuple<int, int, int>(c.Key.Year, c.Key.Month, c.Count()));
            }

            groupedCases = ClosedCases.GroupBy(c => new { c.LastUpDateTime.Year, c.LastUpDateTime.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            ClosedCasesStatistics = new List<Tuple<int, int, int>>();
            foreach (var c in groupedCases)
            {
                ClosedCasesStatistics.Add(new Tuple<int, int, int>(c.Key.Year, c.Key.Month, c.Count()));
            }

        }
        /// <summary>
        /// Gets basis stats from cases based on case status (untouched, open, closed)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public string GetStatsForCase(Tuple<int,int,int> c)
        {
            return $"Date: {c.Item1:D4}.{c.Item2:D2} - Cases: {c.Item3:D5} ";
        }



    }
}
