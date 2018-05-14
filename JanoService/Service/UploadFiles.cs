using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace JanoService.Service
{
    public class ApiJanoService
    {
        private long tramite;
        private readonly string urlUpload;
        private readonly string urlUploadEnd;
        private readonly string urlToken;
        private readonly string tipoTramite;
        private readonly string token;
        public ApiJanoService(long tramite, string tipoTramite)
        {
            this.tramite = tramite;
            this.tipoTramite = tipoTramite;
            urlUpload = Properties.Settings.Default.urlUpload;
            urlUploadEnd = Properties.Settings.Default.urlUploadEnd;
            urlToken = Properties.Settings.Default.urlToken;
            token = getToken();
        }
        public bool UploadFile(string filePath, TypeUpload typeUpload, TipoDato tipoDato)
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
        public bool UploadFileEnd(string correo)
        {
            StringBuilder data = new StringBuilder();
            data.Append($@"{{
                  ""orderActionId"": {tramite.ToString().Substring(0, tramite.ToString().Length - 4)},
                  ""formularyType"": ""{tipoTramite}"",
                  ""email"": ""{correo}"",
                  ""observations"": """",
                  ""isQRValid"": true
                }}");
            byte[] byteArray = Encoding.UTF8.GetBytes(data.ToString());
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlUploadEnd);
            request.Headers["x-ibm-client-id"] = Properties.Settings.Default.clientID;
            request.Headers["Authorization"] = $"Bearer {token}";
            request.Accept = "application/json";
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;
            Stream postStream = request.GetRequestStream();
            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();
            var response = (HttpWebResponse)request.GetResponse();
            var streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            var respuesta = streamReader.ReadToEnd();
            return response.StatusCode == HttpStatusCode.OK;
        }
        public string getToken()
        {
            StringBuilder data = new StringBuilder();
            data.Append($"grant_type={Properties.Settings.Default.grant_type}");
            data.Append($"&client_id={Properties.Settings.Default.clientID}");
            data.Append($"&client_secret={Properties.Settings.Default.clientSecret}");
            data.Append($"&scope={Properties.Settings.Default.scope}");
            byte[] byteArray = Encoding.UTF8.GetBytes(data.ToString());
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlToken);
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
