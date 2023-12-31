using MongoDB.Driver;
using OculusDB.Database;
using OculusGraphQLApiLib.Results;

namespace OculusDB.MongoDB;

/// <summary>
/// Class for caching data when populating multiple objects of an app. This aggregates all offers and version aliases once for an application.
/// </summary>
public class PopulationContext
{
    public List<VersionAlias> versionAliases { get; set; } = new List<VersionAlias>();
    public List<DBOffer> offers { get; set; } = new List<DBOffer>();
    public List<string> alreadySearchedIds { get; set; } = new List<string>();
    public Dictionary<string, DBParentApplication?> parentApplications { get; set; } = new Dictionary<string, DBParentApplication?>();
    public Dictionary<string, List<string>> applicationGroupings { get; set; } = new Dictionary<string, List<string>>();

    public VersionAlias? GetVersionAlias(string? versionId)
    {
        if (versionId == null) return null;
        return versionAliases.FirstOrDefault(x => x.versionId == versionId);
    }
    
    public List<DBOffer> GetOffers(string? offerId)
    {
        if (offerId == null) return new List<DBOffer>();
        List<DBOffer> found = offers.Where(x => x.id == offerId).ToList();
        if (found.Count == 0 && !alreadySearchedIds.Contains(offerId)) // only search offer if we haven't searched it already
        {
            // If we don't have any offers for that id, we need to search the database
            offers.AddRange(OculusDBDatabase.offerCollection.Find(x => x.id == offerId).ToList());
            alreadySearchedIds.Add(offerId);
            found = offers.Where(x => x.id == offerId).ToList();
        }
        return found;
    }
    
    public List<string> GetAppsInApplicationGrouping(string groupingId)
    {
        if(applicationGroupings.ContainsKey(groupingId))
        {
            return applicationGroupings[groupingId];
        }

        applicationGroupings.Add(groupingId, DBApplicationGrouping.GetApplicationIdsFromGrouping(groupingId));
        return applicationGroupings[groupingId];
    }

    public DBParentApplication? GetParentApplication(string id)
    {
        if (!parentApplications.ContainsKey(id))
        {
            DBApplication? app = DBApplication.ById(id);
            if (app != null)
            {
                DBParentApplication? parentApp = new DBParentApplication();
                parentApp.__lastUpdated = app.__lastUpdated;
                parentApp.id = app.id;
                parentApp.displayName = app.displayName;
                parentApplications.Add(id, parentApp);
            }
            else
            {
                parentApplications.Add(id, null);
            }
        }
        return parentApplications[id];
    }
    
    public List<DBParentApplication> GetParentApplicationInApplicationGrouping(string groupingId)
    {
        List<DBParentApplication> parentApps = new List<DBParentApplication>();
        foreach(string appId in GetAppsInApplicationGrouping(groupingId))
        {
            parentApps.Add(GetParentApplication(appId));
        }
        return parentApps;
    }

    public static PopulationContext GetForApplication(string appId)
    {
        PopulationContext context = new PopulationContext();
        context.versionAliases = OculusDBDatabase.versionAliases.Find(x => x.appId == appId).ToList();
        context.offers = OculusDBDatabase.offerCollection.Find(x => x.grouping != null && x.grouping.applicationIds.Contains(appId)).ToList();
        return context;
    }
    
    public static PopulationContext GetForApplications(List<string> appIds)
    {
        PopulationContext context = new PopulationContext();
        context.versionAliases = OculusDBDatabase.versionAliases.Find(x => appIds.Contains(x.appId)).ToList();
        foreach (string appId in appIds)
        {
            // Add every offer for an appid but make sure to not add duplicates
            List<DBOffer> offers = OculusDBDatabase.offerCollection.Find(x => x.grouping != null && x.grouping.applicationIds.Contains(appId)).ToList();
            foreach (DBOffer offer in offers)
            {
                if (offer.GetEntryForDiffGeneration(context.offers) == null) context.offers.Add(offer);
            }
        }
        return context;
    }

    public static PopulationContext GetForApplicationContext(ApplicationContext context)
    {
        return GetForApplications(context.appIds);
    }
}