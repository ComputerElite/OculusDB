using System.Text.Json.Serialization;
using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplication : DBBase, IDBObjectOperations<DBApplication>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Application;

    [BsonIgnore]
    public bool blocked
    {
        get
        {
            return OculusDBDatabase.blockedAppsCache.Contains(id);
        }
    }

    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";

    [BsonIgnore]
    public string imgUrl
    {
        get
        {
            return "/assets/app/" + id;
        }
    }
    [BsonIgnore]
    public string imgUrlAbsolute
    {
        get
        {
            return OculusDBEnvironment.config.publicAddress + "/assets/app/" + id;
        }
    }
    [TrackChanges]
    public HeadsetGroup group { get; set; } = HeadsetGroup.Unknown;
    
    [OculusFieldAlternate("category_enum")]
    [TrackChanges]
    public Category category { get; set; } = Category.UNKNOWN;
    [BsonIgnore]
    [TrackChanges]
    public string categoryFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(category.ToString());
        }
    }
    [JsonIgnore]
    [BsonIgnore]
    public string oculusImageUrl { get; set; } = "";
    [OculusFieldAlternate("genres")]
    [TrackChanges]
    public List<string> genres { get; set; } = new List<string>();
    public List<string> genresFormatted {
        get
        {
            return genres.ConvertAll(x => OculusConverter.FormatOculusEnumString(x));
        }
    }
    
    [OculusField("has_in_app_ads")]
    [TrackChanges]
    public bool hasInAppAds { get; set; } = false;
    
    [OculusField("is_concept")]
    [TrackChanges]
    public bool isAppLab { get; set; } = false;
    
    [OculusField("is_quest_for_business")]
    [TrackChanges]
    public bool isQuestForBusiness { get; set; } = false;
    
    [OculusField("is_test")]
    [TrackChanges]
    public bool isTest { get; set; } = false;
    
    [OculusField("is_blocked_by_verification")]
    [TrackChanges]
    public bool isBlockedByVerification { get; set; } = false;
    
    [OculusField("is_for_oculus_keys_only")]
    [TrackChanges]
    public bool isForOculusKeysOnly { get; set; } = false;
    [TrackChanges]
    public bool isFirstParty { get; set; } = false;
    [TrackChanges]
    public bool cloudBackupEnabled { get; set; } = false;

    [OculusField("releaseDate")]
    [TrackChanges]
    public DateTime? releaseDate { get; set; } = null;
    
    [OculusFieldAlternate("publisher_name")]
    [TrackChanges]
    public string publisherName { get; set; } = "";
    [OculusFieldAlternate("support_website_url")]
    [TrackChanges]
    public string? supportWebsiteUrl { get; set; } = null;
    [OculusFieldAlternate("developer_terms_of_service_url")]
    [TrackChanges]
    public string? developerTermOfServiceUrl { get; set; } = null;
    [OculusFieldAlternate("developer_privacy_policy_url")]
    [TrackChanges]
    public string? developerPrivacyPolicyUrl { get; set; } = null;
    [OculusFieldAlternate("website_url")]
    [TrackChanges]
    public string? websiteUrl { get; set; } = null;
    [OculusFieldAlternate("external_subscription_type_enum")]
    [TrackChanges]
    public ExternalSubscriptionType externalSubscriptionType { get; set; } = ExternalSubscriptionType.UNKNOWN;
    [BsonIgnore]
    public string externalSubscriptionTypeFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(externalSubscriptionType.ToString());
        }
    }

    [OculusFieldAlternate("comfort_rating_enum")]
    [TrackChanges]
    public ComfortRating comfortRating { get; set; } = ComfortRating.UNKNOWN;
    
    [BsonIgnore]
    public string comfortRatingFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(comfortRating.ToString());
        }
    }
    
    [TrackChanges]
    public string? offerId { get; set; } = null;
    [OculusFieldAlternate("supported_in_app_languages")]
    [TrackChanges]
    public List<string> supportedInAppLanguages { get; set; } = new List<string>();
    [OculusFieldAlternate("supported_input_devices_enum")]
    [TrackChanges]
    public List<SupportedInputDevice> supportedInputDevices { get; set; } = new List<SupportedInputDevice>();
    [BsonIgnore]
    public List<string> supportedInputDevicesFormatted
    {
        get
        {
            List<string> formatted = new List<string>();
            foreach (SupportedInputDevice device in supportedInputDevices)
            {
                formatted.Add(OculusConverter.FormatOculusEnumString(device.ToString()));
            }
            return formatted;
        }
    }
    
    [OculusFieldAlternate("supported_player_modes_enum")]
    [TrackChanges]
    public List<SupportedPlayerMode> supportedPlayerModes { get; set; } = new List<SupportedPlayerMode>();
    [BsonIgnore]
    public List<string> supportedPlayerModesFormatted
    {
        get
        {
            List<string> formatted = new List<string>();
            foreach (SupportedPlayerMode mode in supportedPlayerModes)
            {
                formatted.Add(OculusConverter.FormatOculusEnumString(mode.ToString()));
            }
            return formatted;
        }
    }
    
    [OculusFieldAlternate("user_interaction_modes_enum")]
    [TrackChanges]
    public List<UserInteractionMode> userInteractionModes { get; set; } = new List<UserInteractionMode>();
    [BsonIgnore]
    public List<string> userInteractionModesFormatted
    {
        get
        {
            List<string> formatted = new List<string>();
            foreach (UserInteractionMode mode in userInteractionModes)
            {
                formatted.Add(OculusConverter.FormatOculusEnumString(mode.ToString()));
            }
            return formatted;
        }
    }
    [TrackChanges]
    public List<ShareCapability> shareCapabilities { get; set; } = new List<ShareCapability>();
    [BsonIgnore]
    public List<string> shareCapabilitiesFormatted
    {
        get
        {
            List<string> formatted = new List<string>();
            foreach (ShareCapability capability in shareCapabilities)
            {
                formatted.Add(OculusConverter.FormatOculusEnumString(capability.ToString()));
            }
            return formatted;
        }
    }
    
    [OculusFieldAlternate("play_area_enum")]
    [TrackChanges]
    public PlayArea playArea { get; set; } = PlayArea.UNKNOWN;
    [BsonIgnore]
    public string playAreaFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(playArea.ToString());
        }
    }
    [JsonIgnore]
    public string searchDisplayName { get; set; } = "";
    [BsonIgnore]
    public string displayName
    {
        get
        {
            return translations.FirstOrDefault(x => x.locale == defaultLocale)?.displayName ?? "";
        }
    }
    [BsonIgnore]
    public string shortDescription
    {
        get
        {
            return translations.FirstOrDefault(x => x.locale == defaultLocale)?.shortDescription ?? "";
        }
    }
    [BsonIgnore]
    public string longDescription
    {
        get
        {
            return translations.FirstOrDefault(x => x.locale == defaultLocale)?.longDescription ?? "";
        }
    }
    [BsonIgnore]
    public bool longDescriptionUsesMarkdown
    {
        get
        {
            return translations.FirstOrDefault(x => x.locale == defaultLocale)?.longDescriptionUsesMarkdown ?? false;
        }
    }
    [BsonIgnore]
    public List<string> keywords
    {
        get
        {
            return translations.FirstOrDefault(x => x.locale == defaultLocale)?.keywords ?? new List<string>();
        }
    }
    
    [TrackChanges]
    public string? packageName { get; set; } = null;
    [TrackChanges]
    public string canonicalName { get; set; } = "";

    [BsonIgnore]
    public List<DBOffer> offers { get; set; } = null;
    
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBApplicationGrouping? grouping { get; set; } = null;
    [ListScrapingNodeFieldPresent]
    [TrackChanges]
    public List<DBApplicationTranslation> translations { get; set; } = new List<DBApplicationTranslation>();
    [TrackChanges]
    public string defaultLocale { get; set; } = "";
    [OculusFieldAlternate("recommended_graphics")]
    [TrackChanges]
    public string? recommendedGraphics { get; set; } = null;
    [OculusFieldAlternate("recommended_processor")]
    [TrackChanges]
    public string? recommendedProcessor { get; set; } = null;
    [OculusFieldAlternate("recommended_memory_gb")]
    [TrackChanges]
    public double? recommendedMemoryGB { get; set; } = null;
    [BsonIgnore]
    public string? recommendedMemoryGBFormatted
    {
        get
        {
            if(recommendedMemoryGB == null) return null;
            return SizeConverter.GigaByteSizeToString(recommendedMemoryGB.Value);
        }
    }
    
    [TrackChanges]
    public bool hasUnpublishedMetadataInQueue { get; set; } = false;
    
    public List<DBError> errors { get; set; } = new List<DBError>();
    
    public DBApplication GetEntryForDiffGeneration(IEnumerable<DBApplication> collection)
    {
        return collection.FirstOrDefault(x => x.id == this.id);
    }
    public DBApplication GetEntryForDiffGenerationFromDB()
    {
        return OculusDBDatabase.applicationCollection.Find(x => x.id == this.id).FirstOrDefault();
    }

    public void AddOrUpdateEntry(IMongoCollection<DBApplication> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }

    /// <summary>
    /// Gets an DB application by id from the database
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static DBApplication? ById(string? appid)
    {
        if (appid == null) return null;
        return OculusDBDatabase.applicationCollection.Find(x => x.id == appid).FirstOrDefault();
    }
    
    public static DBApplication? ByPackageName(string? packageName)
    {
        if(packageName == null) return null;
        return OculusDBDatabase.applicationCollection.Find(x => x.packageName == packageName).FirstOrDefault();
    }

    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromAppId(id);
    }

    public override void PopulateSelf(PopulationContext context)
    {
        offers = context.GetOffers(offerId);
    }

    public override string GetId()
    {
        return id;
    }
}