using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class WebAccessClient
    {
        private readonly HttpClient _client;
        private readonly string _targetSite;

        /// <summary>
        /// Sets up WebAccess instance
        /// </summary>
        /// <param name="targetSite"></param>
        public WebAccessClient(string targetSite)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            _client = new HttpClient(clientHandler);
            _client.DefaultRequestHeaders.ConnectionClose = true;
            _targetSite = targetSite;
        }

        /// <summary>
        /// Check if the connection to site is possible
        /// </summary>
        /// <returns></returns>
        public async Task<string> CheckInternet()
        {
            string output = "Connection Failed";
            var connection = await _client.GetAsync(_targetSite);

            if (connection.IsSuccessStatusCode)
            {
                output = "Connection was successful";
            }

            return output;
        }

        /// <summary>
        /// Get Information for ID from TargetSite 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Tuple<string, string>> GetIdAsync(string fieldName, string id)
        {
            Tuple<string, string> output;

            string site = $"{_targetSite}?{fieldName}={id}";
            var response = "";
            while (response == "")
            {
                try
                {
                    response = await _client.GetStringAsync(site);
                }
                catch (Exception e)
                {
                    //Catch exception
                }
            }

            output = new Tuple<string, string>(id, response.ToString());
            return output;
        }

        /// <summary>
        /// Get Information for List of IDs for the Target site
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="idList"></param>
        /// <returns></returns>
        public async Task<List<Tuple<string, string>>> GetListOfIdAsync(string fieldName, List<string> idList)
        {
            List<Tuple<string, string>> htmlList = new List<Tuple<string, string>>();

            foreach (var id in idList)
            {
                htmlList.Add(await GetIdAsync(fieldName, id));
            }

            return htmlList;
        }
    }
}
