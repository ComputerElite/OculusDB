using System.Net;
using System.Text.Json;
using ComputerUtils.Logging;

namespace OculusDB.ApiDocs;

public class ApiDocsCreator
{
    public static void UpdateApiDocs()
    {
        ApiDocsRoot apiDocsRoot =
            JsonSerializer.Deserialize<ApiDocsRoot>(File.ReadAllText(FrontendServer.frontend + "api_raw.json"));
        Logger.Log(JsonSerializer.Serialize(apiDocsRoot));
        WebClient webClient = new WebClient();
        for (int i = 0; i < apiDocsRoot.endpoints.Count; i++)
        {
            if (apiDocsRoot.endpoints[i].exampleUrl == "" || apiDocsRoot.endpoints[i].method != "GET") continue;
            
            string path = FrontendServer.config.publicAddress + apiDocsRoot.endpoints[i].exampleUrl.Substring(1);
            Logger.Log("Generating example response for " + path);
            try {
                string res = webClient.DownloadString(path);
            } catch(Exception e) {
                Logger.Log("Error while getting example request" + e);
                continue;
            }
            if(res.StartsWith("{") || res.StartsWith("["))
                apiDocsRoot.endpoints[i].exampleResponse = JsonSerializer.Deserialize<dynamic>(res);
            else apiDocsRoot.endpoints[i].exampleResponse = res;
        }
        Logger.Log("Saving updated api.json");
        File.WriteAllText(FrontendServer.frontend + "api.json", JsonSerializer.Serialize(apiDocsRoot));
    }
}
