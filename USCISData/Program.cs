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


            var listOfCases = sql.GetListOfCaseIdsByForm("I-129F").GetRange(0, 2985);
            var listOfDownloadedCases = await GetCasesFromWebSiteParallelAsync(c, listOfCases, 100);
            GetOpenCases(listOfDownloadedCases);
            UpdateCaseStatus(sql, listOfDownloadedCases, "I-129F");


            Console.WriteLine("Done");
            Console.ReadKey();
        }


        /// <summary>
        /// Static Members 
        /// </summary>
        static readonly WebAccessClient webConnectionCheck = new WebAccessClient("https://egov.uscis.gov/casestatus/mycasestatus.do");
        static List<WebAccessClient> _webConnections = new List<WebAccessClient>();
        
        
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
            
            var groupedCases = untouchedCases.GroupBy(c => new { c.LastUpDateTime.Year, c.LastUpDateTime.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            Console.WriteLine("Untouched cases per Month (contains only Case Was Received status):");
            foreach (var c in groupedCases)
            {
                Console.WriteLine(c.Key + " " + c.Count());
            }
            Console.WriteLine("Total Num of Untouched Cases: " + untouchedCases.Count);
            Console.WriteLine();
            Console.WriteLine();

            groupedCases = openCases.GroupBy(c => new{c.LastUpDateTime.Year, c.LastUpDateTime.Month}).OrderBy(c => c.Key.Month ).OrderBy(c => c.Key.Year).ToList();
            Console.WriteLine("Open cases per Month (RFE, Initial Evidence and other):");
            foreach (var c in groupedCases)
            {
                Console.WriteLine(c.Key +" "+ c.Count());
            }
            Console.WriteLine("Total Num of Open Cases: " + openCases.Count);
            Console.WriteLine();
            Console.WriteLine();

            groupedCases = closedCases.GroupBy(c => new { c.LastUpDateTime.Year, c.LastUpDateTime.Month }).OrderBy(c => c.Key.Month).OrderBy(c => c.Key.Year).ToList();
            Console.WriteLine("Closed Cases per Month (Denials, rejections, withdrawals and other):");
            foreach (var c in groupedCases)
            {
                Console.WriteLine(c.Key + " " + c.Count());
            }
            Console.WriteLine("Total Num of Closed Cases: " + closedCases.Count);

        }
        /// <summary>
        /// Update Form Types for CaseIDs DB
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        public static void UpdateFormType(SqliteCrud sql, List<FullCaseModel> list)
        {
            foreach (var item in list)
            {
                sql.UpdateFormTypeForCaseIds(item.Id, item.FormType);
            }
        }

        /// <summary>
        /// Update case statuses in the DB
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="listOfCases"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static void UpdateCaseStatus(SqliteCrud sql, List<FullCaseModel> listOfCases, string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();
            //get list of cases from the db
            List<FullCaseModel> listOfStatuses = FilterStatusByForm(sql, formType);
            
            //compare if there are members of listOfCases that are not contained in list of cases in db
            foreach (var item in listOfCases)
            {
                var temp = listOfStatuses.Where(x => (x.Id == item.Id) && (x.LastUpDateTime==item.LastUpDateTime)).ToList();
                if (temp.Count==0)
                {
                    output.Add(item);
                }
            }

            foreach (var item in output)
            {
                sql.UpsertCase(item);
            }
        }
        

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
        

        /// <summary>
        /// Generates list of cases based on input case
        /// </summary>
        /// <param name="c"></param>
        /// <param name="startCase"></param>
        /// <param name="before"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        public static List<string> GenerateListOfCases(Converter c, string startCase, uint before, uint after)
        {
            List<string> output = new List<string>();
            output = c.GenerateCaseList(c.SplitUscisNum(startCase), before, after);

            return output;
        }
        

        /// <summary>
        /// Get all case of certain form type
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static List<string> FilterCaseIdsByForm(SqliteCrud sql, string formType)
        {
            List<string> output = new List<string>();
            List<BasicCaseModel> listOfCases = sql.GetListOfBasicModelsByForm(formType);

            foreach (var item in listOfCases)
            {
                output.Add(item.Id);
            }

            return output;
        }

        /// <summary>
        /// Get all statuses of certain type
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="formType"></param>
        /// <returns></returns>
        public static List<FullCaseModel> FilterStatusByForm(SqliteCrud sql, string formType)
        {
            List<FullCaseModel> output = new List<FullCaseModel>();
            List<FullCaseModel> listOfCases = sql.GetListOfFullModelsByForm(formType);
            output= listOfCases.OrderBy(c => c.Id).ThenBy(c => c.LastUpDateTime).ToList();
            return output;
        }


        /// <summary>
        /// Update list of case and return new status for the cases
        /// </summary>
        /// <param name="web"></param>
        /// <param name="idList"></param>
        /// <returns></returns>
        public static async Task<List<Tuple<string, string>>> UpdateCaseStatusAsync(WebAccessClient web, List<string> idList)
        {
            // initialize list of tuples to store USCIS feedback
            List<Tuple<string, string>> htmlList = new List<Tuple<string, string>>();
            
            foreach (var id in idList)
            {
                htmlList.Add( await  web.GetIdAsync("appReceiptNum", id));
            }

            return htmlList;
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
