using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.NodeServices {
	internal class HttpNodeHost : OutOfProcessNodeRunner {
        private readonly static Regex PortMessageRegex = new Regex(@"^\[Microsoft.AspNet.NodeServices.HttpNodeHost:Listening on port (\d+)\]$");
        
		private readonly static JsonSerializerSettings jsonSerializerSettings =  new JsonSerializerSettings { 
            ContractResolver = new CamelCasePropertyNamesContractResolver() 
        };
        
        private int _portNumber;
		
		public HttpNodeHost(int port = 0)
            : base(EmbeddedResourceReader.Read(typeof(HttpNodeHost), "/Content/Node/entrypoint-http.js"), port.ToString())
        {
		}
        
        public override async Task<string> Invoke(NodeInvocationInfo invocationInfo) {
            await this.EnsureReady();
            
            using (var client = new HttpClient()) {
                // TODO: Use System.Net.Http.Formatting (PostAsJsonAsync etc.)
                var payloadJson = JsonConvert.SerializeObject(invocationInfo, jsonSerializerSettings);
                var payload = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("http://localhost:" + this._portNumber, payload);
                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
        }
        
        protected override void OnOutputDataReceived(string outputData) {
            var match = this._portNumber != 0 ? null : PortMessageRegex.Match(outputData);
            if (match != null && match.Success) {
                this._portNumber = int.Parse(match.Groups[1].Captures[0].Value);
            } else {
                base.OnOutputDataReceived(outputData);
            }
        }
        
        protected override void OnBeforeLaunchProcess() {
            // Prepare to receive a new port number
            this._portNumber = 0;
        }
    }
}
