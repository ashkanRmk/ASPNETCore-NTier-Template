using System;
using System.Threading.Tasks;
using Liaro.Common;
using Liaro.ModelLayer;
using Liaro.ServiceLayer.Contracts;
using RestSharp;

namespace Liaro.ServiceLayer
{
    public class KavenegarService : IKavenegarService
    {
        private static RestClient _client;
        private string ApiKey = Environment.GetEnvironmentVariable("Kavenegar_APIKey");
        private string PhoneNumber = Environment.GetEnvironmentVariable("Kavenegar_Number");

        public KavenegarService()
        {
            _client = new RestClient("https://api.kavenegar.com") { Timeout = 50000 };
        }

        public async Task<SmsResultVM> SendLoginCode(string loginCode, string mobile, string fullName)
        {
            return await SendLookup("login", mobile, PhoneCodeType.Sms, fullName, loginCode);
        }

        public async Task<SmsResultVM> SendLookup(string templateName, string receptor, PhoneCodeType type, string token,
            string token2 = null, string token3 = null)
        {
            if (!StringUtils.IsValidPhone(receptor))
            {
                return null;
            }

            if (type == PhoneCodeType.Call && templateName == "")
            {
                return null;
            }

            var request = new RestRequest($"/v1/{ApiKey}/verify/lookup.json");
            request.AddParameter("receptor", receptor);
            request.AddParameter("template", templateName);
            request.AddParameter("type", type == PhoneCodeType.Sms ? "sms" : "call");
            request.AddParameter("token", token);

            if (!string.IsNullOrWhiteSpace(token2)) request.AddParameter("token2", token2);
            if (!string.IsNullOrWhiteSpace(token3)) request.AddParameter("token20", token3);

            var response = await _client.ExecuteTaskAsync<dynamic>(request);
            var tmp = response.Data["return"]["status"];
            return new SmsResultVM()
            {
                @return = new Return()
                {
                    status = (int)tmp
                }
            };
        }
    }
}