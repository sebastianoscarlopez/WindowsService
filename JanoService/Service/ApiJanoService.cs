using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace JanoService.Service
{
    /// <summary>
    /// Jano api's interactor 
    /// </summary>
    public class ApiJanoService
    {
        private long tramite;
        private readonly string urlUpload;
        private readonly string urlUploadEnd;
        private readonly string urlToken;
        private readonly string tipoTramite;
        private readonly string token;
        /// <summary>
        /// Prepare object who must be ready for api
        /// </summary>
        /// <param name="tramite">producto + fecha carga</param>
        /// <param name="tipoTramite">Value from table AppDistribuidores_TiposFormulariosJANO</param>
        public ApiJanoService(long tramite, string tipoTramite)
        {
            this.tramite = tramite;
            this.tipoTramite = tipoTramite;
            urlUpload = Properties.Settings.Default.urlUpload;
            urlUploadEnd = Properties.Settings.Default.urlUploadEnd;
            urlToken = Properties.Settings.Default.urlToken;
            token = getToken();
        }
        /// <summary>
        /// Photo dni or pdf upload
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="typeUpload">Defined by Jano documentation</param>
        /// <param name="tipoDato">Must be FOTO_DNI_FRENTE, FOTO_DNI_DORSO or PDF_FIRMADO</param>
        /// <returns>True whether server return 200</returns>
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
        /// <summary>
        /// Una vez terminado de subir el dni y el pdf firmado, se utiliza este metodo para indicar a TEF que se finalizo.
        /// </summary>
        /// <param name="email">Email from table AppDistribuidores_DatosAdicionalesTramitaciones when IdTipoDato = 5</param>
        /// <param name="tramite">nroProducto from table piezas</param>
        /// <param name="year">FechaCarga from tableOrdenRetiro</param>
        /// <returns>True whether server return 200</returns>
        public bool UploadFileEnd(string email, long tramite, long year)
        {
            StringBuilder data = new StringBuilder();
            data.Append($@"{{
                  ""orderActionId"": {tramite},
                  ""formularyType"": ""{tipoTramite}"",
                  ""email"": ""{email}"",
                  ""observations"": """",
                  ""isQRValid"": true
                }}");
            byte[] byteArray = Encoding.UTF8.GetBytes(data.ToString());
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(urlUploadEnd, tramite, year));
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
        /// <summary>
        /// Get token by OAuth2
        /// </summary>
        /// <returns>Token</returns>
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
