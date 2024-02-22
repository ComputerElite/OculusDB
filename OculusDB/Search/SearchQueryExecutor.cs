using MongoDB.Bson;
using MongoDB.Driver;
using OculusDB.Database;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;

namespace OculusDB.Search;

public class SearchQueryExecutor
{
    public static SearchResult ExecuteQuery(SearchQuery query)
    {
        switch (query.OculusDBType)
        {
            case DBDataTypes.Application:
                return SearchApplications(query);
            case DBDataTypes.Version:
                return SearchVersions(query);
            case DBDataTypes.Achievement:
                return SearchAchievements(query);
            case DBDataTypes.IapItem:
                return SearchIapItems(query);
            case DBDataTypes.IapItemPack:
                return SearchIapItemPacks(query);
            case DBDataTypes.Difference:
                return SearchDifferences(query);
        }

        return new SearchResult("Unknown OculusDBType for search");
    }

    private static SearchResult SearchDifferences(SearchQuery query)
    {
        List<DBDifference> differences = OculusDBDatabase.differenceCollection.Find(x => 
            query.differenceNameTypes.Contains(x.differenceName) &&
            (query.parentApplication == "" || x.entryParentApplicationIds.Contains(query.parentApplication))
        ).Skip(query.skip).Limit(query.limit).ToList();
        return new SearchResult(new List<dynamic>(differences), null);
    }

    private static SearchResult SearchIapItems(SearchQuery query)
    {
        string? grouping = GetGroupingId(query);
        List<DBIapItem> apps = OculusDBDatabase.iapItemCollection.Find(x => 
            (grouping == null || x.grouping != null && x.grouping.id == grouping) 
            &&
            (
                query.searchRegex.IsMatch(x.displayName) ||
                query.searchRegex.IsMatch(x.sku)
            )
        ).Skip(query.skip).Limit(query.limit).ToList();
        PopulationContext c = new PopulationContext();
        apps.ForEach(x => x.PopulateSelf(c));
        return new SearchResult(new List<dynamic>(apps), null);
    }

    private static SearchResult SearchAchievements(SearchQuery query)
    {
        string? grouping = GetGroupingId(query);
        List<DBAchievement> apps = OculusDBDatabase.achievementCollection.Find(x => 
            (grouping == null || x.grouping != null && x.grouping.id == grouping) 
            &&
            (
                (x.searchTitle != null && query.searchRegex.IsMatch(x.searchTitle)) ||
                query.searchRegex.IsMatch(x.apiName)
            )
        ).Skip(query.skip).Limit(query.limit).ToList();
        PopulationContext c = new PopulationContext();
        apps.ForEach(x => x.PopulateSelf(c));
        return new SearchResult(new List<dynamic>(apps), null);
    }

    private static SearchResult SearchVersions(SearchQuery query)
    {
        if(query.parentApplication == "") return new SearchResult("Parent application required for version search");
        List<DBVersion> apps = OculusDBDatabase.versionCollection.Find(x => 
            (x.parentApplication != null && x.parentApplication.id == query.parentApplication) 
            &&
            (
                query.searchRegex.IsMatch(x.filename)  ||
                query.searchRegex.IsMatch(x.version) ||
                query.searchRegex.IsMatch(x.versionCode.ToString())
            )
        ).Skip(query.skip).Limit(query.limit).ToList();
        PopulationContext c = new PopulationContext();
        apps.ForEach(x => x.PopulateSelf(c));
        return new SearchResult(new List<dynamic>(apps), null);
    }

    private static SearchResult SearchApplications(SearchQuery query)
    {
        var filter = Builders<DBApplication>.Filter.And(
            Builders<DBApplication>.Filter.In(x => x.group, query.headsetGroups),
            Builders<DBApplication>.Filter.Or(
                Builders<DBApplication>.Filter.Regex(x => x.searchDisplayName, new BsonRegularExpression(query.searchRegex)),
                Builders<DBApplication>.Filter.Regex(x => x.canonicalName, new BsonRegularExpression(query.searchRegex)),
                Builders<DBApplication>.Filter.Regex(x => x.packageName, new BsonRegularExpression(query.searchRegex)),
                Builders<DBApplication>.Filter.Regex(x => x.publisherName, new BsonRegularExpression(query.searchRegex))
            ),
            Builders<DBApplication>.Filter.ElemMatch(x => x.supportedInAppLanguages, Builders<string>.Filter.In(x => x, query.supportedInAppLanguages))
        );
        List<DBApplication> apps = OculusDBDatabase.applicationCollection.Find(x => 
            query.headsetGroups.Contains(x.group) &&
            (
                query.searchRegex.IsMatch(x.searchDisplayName) ||
                query.searchRegex.IsMatch(x.canonicalName) ||
                (x.packageName != null && query.searchRegex.IsMatch(x.packageName)) ||
                query.searchRegex.IsMatch(x.publisherName)
            )
            && (query.supportedInAppLanguages.Count <= 0)
        ).Skip(query.skip).Limit(query.limit).ToList();
        PopulationContext c = new PopulationContext();
        apps.ForEach(x => x.PopulateSelf(c));
        return new SearchResult(new List<dynamic>(apps), null);
    }

    private static string? GetGroupingId(SearchQuery query)
    {
        string? grouping = null;
        if (query.parentApplication != "")
        {
            grouping = ApplicationContext.FromAppId(query.parentApplication).groupingId;
        }

        return grouping;
    }
    
    private static SearchResult SearchIapItemPacks(SearchQuery query)
    {
        string? grouping = GetGroupingId(query);
        List<DBIapItemPack> apps = OculusDBDatabase.iapItemPackCollection.Find(x => 
            (grouping == null || x.grouping != null && x.grouping.id == grouping) 
            &&
            (
                query.searchRegex.IsMatch(x.displayName)
            )
        ).Skip(query.skip).Limit(query.limit).ToList();
        PopulationContext c = new PopulationContext();
        apps.ForEach(x => x.PopulateSelf(c));
        return new SearchResult(new List<dynamic>(apps), null);
    }
}