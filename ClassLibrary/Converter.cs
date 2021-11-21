using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using HtmlAgilityPack;

namespace ClassLibrary
{
    /// <summary>
    /// Helper Class providing different services such es spitting strings, lists, html data
    /// </summary>
    public class Converter
    {
        /// <summary>
        /// Split USCIS number to alphabetical and numerical part
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Dictionary<string, uint> SplitUscisNum(string id)
        {
            Dictionary<string, uint> output = new Dictionary<string, uint>();
            
            string prefix = id.Substring(0, 3).ToUpper();
            uint suffix = uint.Parse(id.Substring(3));
            
            output.Add(prefix,suffix);
            return output;
        }


        /// <summary>
        /// Generates CaseList that needs to be checked in USCIS system
        /// </summary>
        /// <param name="caseId"></param>
        /// <param name="beforeCase"></param>
        /// <param name="afterCase"></param>
        /// <returns></returns>
        public List<string> GenerateCaseList(Dictionary<string, uint> caseId, uint beforeCase, uint afterCase)
        {
            List<string> output = new List<string>();
            
            string prefix = caseId.Keys.First();
            uint postfix = caseId.Values.First();

            // Calculate Case Nums for target Range
            for (uint i = postfix-beforeCase; i <= postfix + afterCase; i++)
            {
                // check if there is minimum 10 numerical values
                if (i.ToString().Length<10)
                {
                    int missingNums = 10 - i.ToString().Length;
                    string fullString = String.Concat(Enumerable.Repeat("0", missingNums)) + i.ToString();

                    output.Add($"{prefix}{fullString}");
                }
                else
                {
                    output.Add($"{prefix}{i.ToString()}");
                }
            }

            return output;
        }


        /// <summary>
        /// Extract case data from HTML and splits data into separate parts
        /// </summary>
        /// <param name="htmlString"></param>
        public Tuple<string, string, DateTime, string, DateTime, string> ExtractCaseData(string id, string htmlString)
        {
            Tuple<string, string, DateTime, string, DateTime, string> output;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlString);

            // get Case Status and Case information from HTML 
            var htmlStatus = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='" + "current-status-sec" + "']");
            var htmlInfo = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='" + "rows text-center" + "']");
            string caseStatus = htmlStatus.InnerText.Trim();
            string caseInfos = htmlInfo.InnerText.Trim();

            // extract case status
            int temp = caseStatus.IndexOf(":")+1;
            caseStatus = caseStatus.Substring(temp, caseStatus.Length - temp - 1).Trim();

            // split case details to list
            caseInfos = caseInfos.Substring(caseStatus.Length).Trim();
            
            Regex rx = new Regex(@"[A-Z]{3}\d{10}");
            var caseId = rx.Match(caseInfos).ToString();

            if (caseStatus == "" && caseInfos=="" || caseId != id && caseId != "")
            {
                return  new Tuple<string, string, DateTime, string, DateTime, string>(id, "Case doesn't exist", new DateTime(2000,1,1),
                    "N/A", DateTime.Now, "This case number doesn't exist");
            }

            var caseDetails = caseInfos.Substring(2).Split(",").ToList();

            // extract datum
            string lastUpdateString = caseDetails[1] + caseDetails[0];
            DateTime lastUpdateDate;
            DateTime.TryParse(lastUpdateString, out lastUpdateDate);

            // extract form type
            string formType = "N/A";
            if (caseDetails[2].Contains("your Form"))
            {
                temp = caseDetails[2].IndexOf("F");
                formType = caseDetails[2].Substring(temp + 5);
            }

            // time stamp for refresh
            DateTime refreshDate = DateTime.Now;

            output = new Tuple<string, string, DateTime, string, DateTime, string>(id, caseStatus, lastUpdateDate,
                formType, refreshDate, caseInfos);

            return output;
        }

        /// <summary>
        /// Splits list of Type T to requested number of smaller lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputList"></param>
        /// <param name="numOfSplits"></param>
        /// <returns></returns>
        public List<List<T>> SplitList<T>(List<T> inputList, int numOfSplits)
        {
            List<List<T>> output = new List<List<T>>();
            var splits = inputList.Select((item, index) => new { index, item }).GroupBy(x => x.index % numOfSplits).Select(x => x.Select(y => y.item));

            foreach (var item in splits)
            {
                output.Add(item.ToList());
            }
            return output;
        }
    }
}
