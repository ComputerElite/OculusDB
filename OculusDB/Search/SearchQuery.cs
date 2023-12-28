using System.Text.RegularExpressions;
using ComputerUtils.Webserver;
using OculusDB.Api;
using OculusDB.ObjectConverters;

namespace OculusDB.Search;

public class SearchQuery
{
    public List<HeadsetGroup> headsetGroups { get; set; } = new List<HeadsetGroup>();
    public List<DifferenceNameType> differenceNameTypes { get; set; } = new List<DifferenceNameType>();
    public string OculusDBType { get; set; } = "";
    public string searchQuery { get; set; } = "";
    public string differenceTypes { get; set; } = "";
    public Regex searchRegex
    {
        get
        {
            return new Regex(".*" + searchQuery.Replace(" ", ".*") + ".*", RegexOptions.IgnoreCase);
        }
    }
    public string parentApplication { get; set; } = "";
    
    public int skip { get; set; } = 0;
    public int limit { get; set; } = 50;
    

    public static SearchQuery FromRequest(ServerRequest request)
    {
        SearchQuery q = new SearchQuery();
        q.searchQuery = request.queryString.Get("q") ?? "";
        q.OculusDBType = request.queryString.Get("type") ?? "Application";
        q.skip = int.Parse(request.queryString.Get("skip") ?? "0");
        if(q.skip < 0) q.skip = 0;
        q.limit = int.Parse(request.queryString.Get("limit") ?? "100");
        q.parentApplication = request.queryString.Get("appid") ?? "";
        if(q.limit < 0) q.limit = 100;
        string groups = request.queryString.Get("groups") ??
                        String.Join(",", HeadsetIndex.entries.Select(x => x.groupString).Distinct()); // default to all groups if none are given
        foreach (string g in groups.Split(","))
        {
            if(g == "") continue;
            q.headsetGroups.Add(HeadsetIndex.ParseGroup(g));
        }
        string differenceTypes = request.queryString.Get("differenceNameTypes") ?? EnumIndex.AllEnumNamesDifferenceNameTypes();
        foreach (string type in differenceTypes.Split(','))
        {
            q.differenceNameTypes.Add(EnumIndex.parseEnumDifferenceNameType(type));
        }
        return q;
    }
}