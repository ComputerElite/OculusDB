using System.IO.Compression;
using System.Net;
using System.Text.Json;
using ComputerUtils.Webserver;
using OculusDB.QAVS;

namespace OculusDB.Api;

public class BeatSaberApi
{
    public static void SetupRoutes(HttpServer server)
    {
        
        server.AddRouteRedirect("GET", "/api/coremodsdownload", "/api/v2/games/2448060205267927/coremodsdownload", true);
        server.AddRoute("GET", "/api/v2/games/2448060205267927/coremodsdownload", new Func<ServerRequest, bool>(request =>
        {
	        string v = request.pathDiff.Replace(".qmod", "");
			Dictionary<string, CoreMods> mods = JsonSerializer.Deserialize<Dictionary<string, CoreMods>>(File.ReadAllText(OculusDBEnvironment.dataDir + "coremods.json"));
            if(mods.ContainsKey(v))
            {
                CoreMods used = mods[v];
                QMod mod = new QMod();
				mod.name = "Core mods for " + v;
                mod.id = "OculusDB_CoreMods_" + v;
                mod.packageVersion = v;
                mod.description = "Downloads all Core mods for Beat Saber version " + v;
				foreach (CoreMod m in used.mods)
                {
                    mod.dependencies.Add(new QModDependency { downloadIfMissing = m.downloadLink, id = m.id, version = "^" + m.version });
                }
                MemoryStream stream = new MemoryStream();
                ZipArchive a = new ZipArchive(stream, ZipArchiveMode.Create, true);
                StreamWriter writer = new StreamWriter(a.CreateEntry("mod.json").Open());
                writer.Write(JsonSerializer.Serialize(mod));
                writer.Flush();
                writer.Close();
                writer.Dispose();
                a.Dispose();
                request.SendData(stream.ToArray(), "application/zip", 200, true, new Dictionary<string, string> { { "Content-Disposition", "inline; filename=\"OculusDB_CoreMods.qmod\"" } });
			} else
            {
                request.SendString("", "text/plain", 404);
            }
			return true;
        }), true, true, true, true, 300); // 5 mins
        server.AddRouteRedirect("GET", "/api/coremodsproxy", "/api/v2/games/2448060205267927/coremodsproxy");
        server.AddRoute("GET", "/api/v2/games/2448060205267927/coremodsproxy", new Func<ServerRequest, bool>(request =>
        {
            WebClient webClient = new WebClient();
            try
            {
                string res = webClient.DownloadString("https://git.bmbf.dev/unicorns/resources/-/raw/master/com.beatgames.beatsaber/core-mods.json");
                if (res.Length <= 2) throw new Exception("lol fuck you idiot");
                request.SendString(res, "application/json", 200, true, new Dictionary<string, string>
                {
                    {
                        "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                    }
                });
                File.WriteAllText(OculusDBEnvironment.dataDir + "coremods.json", res);
            }
            catch (Exception e)
            {
                if(File.Exists(OculusDBEnvironment.dataDir + "coremods.json"))
                {
                    if(!request.closed) request.SendString(File.ReadAllText(OculusDBEnvironment.dataDir + "coremods.json"), "application/json", 200, true, new Dictionary<string, string>
                    {
                        {
                            "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                        }
                    });
                } else
                {
                    if(!request.closed) request.SendString("{}", "application/json", 500, true, new Dictionary<string, string>
                    {
                        {
                            "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                        }
                    });
                }
            }

            return true;
        }));
    }
}