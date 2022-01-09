using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using Microsoft.Extensions.Configuration;



namespace USCISData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            
            Console.WriteLine(await webConnectionCheck.CheckInternet());

            Converter c = new Converter();
            SqliteCrud sql = new SqliteCrud(GetConnectionString());
            
            //var listOfCases = GenerateListOfCases(c, "WAC2190093072", 1, 1);
            //var listOfCases = sql.GetListOfCaseIdsByForm("I-129F").GetRange(0, 29854);

            var listOfCases = sql.GetListOfCaseIdsByForm("I-129F");
            var listOfDownloadedCases = await GetCasesFromWebSiteParallelAsync(c, listOfCases, 250);

            DataStatisticsModel statistics = new DataStatisticsModel(listOfDownloadedCases);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Untouched - Cases that only have NOA1 status -> if noa was in Jan, than case is in Jan");
            foreach (var caseStats in statistics.UntouchedCasesStatistics)
            {
                Console.WriteLine(statistics.GetStatsForCase(caseStats));
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Open - any other status than NOA1 or fully closes cases");
            Console.WriteLine("It shows monthly processing rate -> month when new status was applied");
            foreach (var caseStats in statistics.OpenCasesStatistics)
            {
                Console.WriteLine(statistics.GetStatsForCase(caseStats));
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Closed - approvals, denials, withdrawals and any other status indicating that case if closed");
            Console.WriteLine("It shows monthly processing rate -> month when the case was closed");
            foreach (var caseStats in statistics.ClosedCasesStatistics)
            {
                Console.WriteLine(statistics.GetStatsForCase(caseStats));
            }
            sql.UpdateCaseStatus(listOfDownloadedCases, "I-129F");

            Console.WriteLine("Done");
            Console.ReadKey();
        }


        /// <summary>
        /// Static Members 
        /// </summary>
        static readonly WebAccessClient webConnectionCheck = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
        static List<WebAccessClient> _webConnections = new List<WebAccessClient>();


        /// <summary>
        /// Get connection string 
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionStringName = "Default")
        {
            string output = "";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            output = config.GetConnectionString(connectionStringName);

            return output;
        }

        /////////////////////////////////


        public static void StatisticsListCaseGroupedByStatus(List<FullCaseModel> listOfItems)
        {
            var groupedCases = listOfItems.GroupBy(c => new { c.LastStatusChange.Year, c.LastStatusChange.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            foreach (var c in groupedCases)
            {
                Console.WriteLine(c.Key + " " + c.Count());
            }
        }
        public static void StatisticsListCaseGroupedByUscisNum(List<FullCaseModel> listOfItems)
        {
            var casesGroupedByUscisNum = listOfItems.GroupBy(c => new string(c.Id.Substring(0, 10)));
            casesGroupedByUscisNum = casesGroupedByUscisNum.OrderBy(c => c.Key);
            foreach (var c in casesGroupedByUscisNum)
            {
                Console.WriteLine(c.Key + " " + c.Count());
            }
        }
        public static void StatisticsListCase(List<FullCaseModel> cases, List<FullCaseModel> openCases)
        {
            var casesGroupedByUscisNum = cases.GroupBy(c => new string(c.Id.Substring(0, 10)));
            casesGroupedByUscisNum = casesGroupedByUscisNum.OrderBy(c => c.Key);

            var openCasesGroupedByUscisNum = cases.GroupBy(c => new string(c.Id.Substring(0, 10)));

            Console.WriteLine("Total Cases " + " " + "Number");

            foreach (var c in casesGroupedByUscisNum)
            {
                var openCaseStats = openCasesGroupedByUscisNum.Where(oc => oc.Key==c.Key).FirstOrDefault();
                Console.WriteLine(c.Key + " " + c.Count() + " " + openCaseStats.Key + " " + openCaseStats.Count());
            }
        }
        /// <summary>
        /// Download data from USCIS website
        /// </summary>
        /// <param name="c"></param>
        /// <param name="listOfCaseNums"></param>
        /// <param name="numOfTasks"></param>
        /// <returns></returns>
        public static async Task<List<FullCaseModel>> GetCasesFromWebSiteParallelAsync(Converter c, List<string> listOfCaseNums, int numOfTasks)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();

            var splits = c.SplitList(listOfCaseNums, numOfTasks);

            List<Task<List<Tuple<string,string>>>> list = new List<Task<List<Tuple<string,string>>>>();

            foreach (var item in splits)
            {
                WebAccessClient newWebForTask = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
                _webConnections.Add(newWebForTask);
                list.Add( newWebForTask.GetListOfIdAsync("appReceiptNum", item));
            }
            
            var results = await Task.WhenAll(list);
            var htmlList = results.Aggregate(new List<Tuple<string,string>>(), (x, y) => x.Concat(y).ToList());
            
            List<FullCaseModel> caseList = new List<FullCaseModel>();
            foreach (var html in htmlList)
            {
                caseList.Add(new FullCaseModel(c.ExtractCaseData(html.Item1, html.Item2)));
            }
            
            output = caseList;

            return output;
        }
    }
}
