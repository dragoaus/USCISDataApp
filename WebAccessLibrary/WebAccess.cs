using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebAccessLibrary
{

    public class WebAccess
    {
        private HttpClient _client;
        private readonly string _targetSite;

        public WebAccess(string targetSite)
        {
            _client = new HttpClient();
            _targetSite = targetSite;
        }

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

        public async Task<string> GetAllContent()
        {
            string output = "";

            var content = await _client.GetStringAsync(_targetSite);
            output = content;
            return output;
        }

        public async Task<string> HeadRequest()
        {
            string output = "";

            var content = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, _targetSite));
            output = content.ToString();

            return output;
        }

        public async Task PostId(string fieldName, string id)
        {
            var values = new Dictionary<string, string>
            {
                {fieldName, id}
            };
            var data = new FormUrlEncodedContent(values);
            var response = await _client.PostAsync(_targetSite, data);
            var content = await response.Content.ReadAsStringAsync();
        }

        public async Task<Tuple<string,string>> GetIdAsync(string fieldName, string id)
        {
            Tuple<string, string> output;
            
            string site = $"{_targetSite}?{fieldName}={id}";
            var response = await _client.GetStringAsync(site);

            output = new Tuple<string, string>(id, response.ToString());

            return  output;
        }

    }
}
