using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace JanoService.Service
{
    public class UploadFiles
    {
        private long tramite;
        private readonly string urlUpload;
        private readonly string urlToken;
        private readonly string tipoTramite;
        private readonly string token;
        public UploadFiles(long tramite, string tipoTramite, string urlUpload, string urlToken)
        {
            this.tramite = tramite;
            this.urlUpload = urlUpload;
            this.urlToken = urlToken;
            this.tipoTramite = tipoTramite;
            this.token = getToken();
        }
        public bool uploadFile(string filePath, TypeUpload typeUpload, TipoDato tipoDato)
        {
            string fileName = new FileInfo(filePath).Name;
            var size = new FileInfo(filePath).Length;
            var httpForm = new HttpForm(urlUpload)
                            .SetHeader("x-ibm-client-id", "e9553aa7-b1cc-4fd2-a664-deaeb26543cc")
                            .SetHeader("Authorization", $"Bearer {token}")
                            .SetValue("documentMetadata",
            $@"{{ ""name"":""{fileName}"",
              ""description"":""{Enum.GetName(typeof(TipoDato), tipoDato)}"",
			  ""type"":""{(int)typeUpload}"",
			  ""version"":""1"",
			  ""relatedObjects"":[
								{{""entityType"":""tramite"",     ""id"":""{tramite}""}},
								{{""entityType"":""tipoTramite"", ""id"":""{tipoTramite}""}}
							   ],
			  ""attachments"":[
							  {{
								""id"":""{fileName}"",
								""name"":""{fileName}"",
								""description"":""{Enum.GetName(typeof(TipoDato), tipoDato)}"",
								""mimeType"":""{ ((typeUpload == TypeUpload.Image) ? "image/jpeg" : "application/pdf") }"",
								""size"":{size},
								""sizeUnit"":""bytes""
							  }}
							]
			}}")
                            .AttachFile("file", filePath);

            var response = httpForm.Submit();
            return response.StatusCode == HttpStatusCode.OK;
        }
        public string getToken()
        {
            string url = urlToken;
            string clientID = @"e9553aa7-b1cc-4fd2-a664-deaeb26543cc";
            string clientSecret = @"oX5fP5kH7uK1uV2fF7sX0qK3qF3oI6wX4jV5yX7gT5hK1dW4eG";
            string scope = "scope1";

            StringBuilder data = new StringBuilder();
            data.Append("grant_type=client_credentials");
            data.Append($"&client_id={clientID}");
            data.Append($"&client_secret={clientSecret}");
            data.Append($"&scope={scope}");
            byte[] byteArray = Encoding.UTF8.GetBytes(data.ToString());
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "application/json";
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream postStream = request.GetRequestStream();
            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();
            var response = (HttpWebResponse)request.GetResponse();
            var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            var respuesta = streamReader.ReadToEnd();
            return new Regex("\"access_token\":\"([^\"]+)\"").Match(respuesta).Groups[1].Captures[0].Value;
        }
    }
}
