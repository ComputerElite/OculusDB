using MongoDB.Driver;
using OculusDB.Database;

namespace OculusDB.MongoDB;

/// <summary>
/// Class for caching data when populating multiple objects of an app. This aggregates all offers and version aliases once for an application.
/// </summary>
public class PopulationContext
{
    public List<VersionAlias> versionAliases { get; set; } = new List<VersionAlias>();
    public List<DBOffer> offers { get; set; } = new List<DBOffer>();
    public List<string> alreadySearchedIds { get; set; } = new List<string>();
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

    public static PopulationContext GetForApplication(string appId)
    {
        PopulationContext context = new PopulationContext();
        context.versionAliases = OculusDBDatabase.versionAliases.Find(x => x.appId == appId).ToList();
        context.offers = OculusDBDatabase.offerCollection.Find(x => x.parentApplication != null && x.parentApplication.id == appId).ToList();
    }
    
    public static PopulationContext GetForApplications(List<string> appIds)
    {
        PopulationContext context = new PopulationContext();
        context.versionAliases = OculusDBDatabase.versionAliases.Find(x => appIds.Contains(x.appId)).ToList();
        context.offers = OculusDBDatabase.offerCollection.Find(x => x.parentApplication != null && appIds.Contains(x.parentApplication.id)).ToList();
    }

    public static PopulationContext GetForApplicationContext(ApplicationContext context)
    {
        return GetForApplications(context.appIds);
    }
}