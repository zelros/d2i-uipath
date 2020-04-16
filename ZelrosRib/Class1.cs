using System;
using System.Collections.Generic;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Zelros
{
    public class AnalyseRib : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<String> FilePath { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<String> Token { get; set; }


        private InArgument<String> baseUrl = "https://documents.zelros.com";

        [Category("Input")]
        [RequiredArgument]
        public InArgument<String> BaseUrl {
            get { return baseUrl ?? "https://documents.zelros.com"; }
            set { baseUrl = value; }
        }

        [Category("Output")]
        public OutArgument<String> Bic { get; set; }

        [Category("Output")]
        public OutArgument<String> Iban { get; set; }

        [Category("Output")]
        public OutArgument<String> Name { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token.Get(context)}");
            client.Timeout = TimeSpan.FromMinutes(10);

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("type", "RIB");

            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(File.ReadAllBytes(FilePath.Get(context)));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "file", Path.GetFileName(FilePath.Get(context)));
            form.Add(new StringContent("RIB"), "type");

            var url = $"{BaseUrl.Get(context)}/api/analyze";
            HttpResponseMessage response = client.PostAsync(url, form).Result;
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            var result = response.Content.ReadAsStringAsync().Result;
            JObject obj = JObject.Parse(result);

            JArray values = (JArray)obj["results"][0]["result"];

            foreach (JObject val in values)
            {
                var code = (string)val["code"];
                var value = (string)val["value"];
                if (code == "BIC")
                {
                    Bic.Set(context, value);
                }
                else if (code == "IBAN")
                {
                    Iban.Set(context, value);
                }
                else if (code == "name")
                {
                    Name.Set(context, value);
                }
            }
        }
    }
}
