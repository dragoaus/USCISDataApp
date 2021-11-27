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

            var listOfCases = sql.GetListOfCaseIdsByForm("I-129F").GetRange(0, 29854);
            var listOfDownloadedCases = await GetCasesFromWebSiteParallelAsync(c, listOfCases, 50);
            GetOpenCases(listOfDownloadedCases);
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
        public static void GetOpenCases(List<FullCaseModel> cases)
        {
            List<FullCaseModel> untouchedCases = cases.Where(c => c.CaseStatus == "Case Was Received").ToList();

            List<FullCaseModel> openCases = cases.Where(c => c.CaseStatus == "Name Was Updated" ||
                                                             c.CaseStatus == "Request for Additi onal Evidence Was Sent" ||
                                                             c.CaseStatus == "Request for Initial Evidence Was Sent" ||
                                                             c.CaseStatus == "Response To USCIS&#039; Request For Evidence Was Received").ToList();
            
            List<FullCaseModel> closedCases = cases.Where(c => c.CaseStatus == "Case Was Approved" ||
                                                               c.CaseStatus== "Case Was Denied" ||
                                                               c.CaseStatus== "Case Closed Benefit Received By Other Means" ||
                                                               c.CaseStatus== "Case Rejected Because I Sent An Incorrect Fee" ||
                                                               c.CaseStatus== "Case Was Rejected Because I Did Not Sign My Form" ||
                                                               c.CaseStatus== "Case Was Rejected Because It Was Improperly Filed" ||
                                                               c.CaseStatus== "Notice Explaining USCIS Actions Was Mailed" ||
                                                               c.CaseStatus == "Withdrawal Acknowledgement Notice Was Sent").ToList();
            
            Console.WriteLine("Untouched cases per Month (contains only Case Was Received status):");
            StatisticsListCaseGroupedByStatus(untouchedCases);
            Console.WriteLine("Total Num of Untouched Cases: " + untouchedCases.Count);
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("Open cases per Month (RFE, Initial Evidence and other):");
            StatisticsListCaseGroupedByStatus(openCases);
            Console.WriteLine("Total Num of Open Cases: " + openCases.Count);
            Console.WriteLine();
            Console.WriteLine();


            Console.WriteLine("Closed Cases per Month (Denials, rejections, withdrawals and other):");
            StatisticsListCaseGroupedByStatus(closedCases);
            Console.WriteLine("Total Num of Closed Cases: " + closedCases.Count);
            Console.WriteLine();
            Console.WriteLine();

            
            Console.WriteLine("Cases grouped by case number:");
            StatisticsListCaseGroupedByUsicisNum(cases);

            
        }

        public static void StatisticsListCaseGroupedByStatus(List<FullCaseModel> listOfItems)
        {
            var groupedCases = listOfItems.GroupBy(c => new { c.LastUpDateTime.Year, c.LastUpDateTime.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            foreach (var c in groupedCases)
            {
                Console.WriteLine(c.Key + " " + c.Count());
            }
        }
        public static void StatisticsListCaseGroupedByUsicisNum(List<FullCaseModel> listOfItems)
        {
            var casesGroupedByUsicsNum = listOfItems.GroupBy(c => new string(c.Id.Substring(0, 10)));
            casesGroupedByUsicsNum = casesGroupedByUsicsNum.OrderBy(c => c.Key);
            foreach (var c in casesGroupedByUsicsNum)
            {
                Console.WriteLine(c.Key + " " + c.Count());
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
