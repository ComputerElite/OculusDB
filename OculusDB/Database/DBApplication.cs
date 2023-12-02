using System.Text.Json.Serialization;
using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplication : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Application;
    [OculusField("id")]
    public string id { get; set; } = "";
    public HeadsetGroup group { get; set; } = HeadsetGroup.Unknown;
    
    [OculusFieldAlternate("category_enum")]
    public Category category { get; set; } = Category.UNKNOWN;
    [BsonIgnore]
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
    [BsonIgnore]
    [JsonIgnore]
    private List<string> _genres = new List<string>();
    [OculusFieldAlternate("genres")]
    public List<string> genres {
        get
        {
            return _genres.ConvertAll(x => OculusConverter.FormatOculusEnumString(x));
        }
        set => _genres = value;
        
    }
    
    [OculusField("has_in_app_ads")]
    public bool hasInAppAds { get; set; } = false;
    
    [OculusField("is_concept")]
    public bool isAppLab { get; set; } = false;
    
    [OculusField("is_quest_for_business")]
    public bool isQuestForBusiness { get; set; } = false;
    
    [OculusField("is_test")]
    public bool isTest { get; set; } = false;
    
    [OculusField("is_blocked_by_verification")]
    public bool isBlockedByVerification { get; set; } = false;
    
    [OculusField("is_for_oculus_keys_only")]
    public bool isForOculusKeysOnly { get; set; } = false;
    public bool isFirstParty { get; set; } = false;
    
    [OculusField("releaseDate")]
    public DateTime releaseTime { get; set; } = DateTime.MinValue;
    
    [OculusFieldAlternate("publisher_name")]
    public string publisherName { get; set; } = "";
    [OculusFieldAlternate("support_website_url")]
    public string? supportWebsiteUrl { get; set; } = null;
    [OculusFieldAlternate("developer_terms_of_service_url")]
    public string? developerTermOfServiceUrl { get; set; } = null;
    [OculusFieldAlternate("developer_privacy_policy_url")]
    public string? developerPrivacyPolicyUrl { get; set; } = null;
    [OculusFieldAlternate("website_url")]
    public string? websiteUrl { get; set; } = null;
    [OculusFieldAlternate("external_subscription_type_enum")]
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
    public ComfortRating comfortRating { get; set; } = ComfortRating.UNKNOWN;
    
    [BsonIgnore]
    public string comfortRatingFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(comfortRating.ToString());
        }
    }
    
    public string? offerId { get; set; } = null;
    [OculusFieldAlternate("supported_in_app_languages")]
    public List<string> supportedInAppLanguages { get; set; } = new List<string>();
    [OculusFieldAlternate("supported_input_devices_enum")]
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
    
    [OculusFieldAlternate("play_area_enum")]
    public PlayArea playArea { get; set; } = PlayArea.UNKNOWN;
    [BsonIgnore]
    public string playAreaFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(playArea.ToString());
        }
    }

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
    
    
    public string packageName { get; set; } = "";

    [BsonIgnore]
    public List<DBPrice> prices { get; set; } = new List<DBPrice>();
    
    public DBApplicationGrouping grouping { get; set; } = new DBApplicationGrouping();
    public List<DBApplicationTranslation> translations { get; set; } = new List<DBApplicationTranslation>();
    public string defaultLocale { get; set; } = "";
    [OculusFieldAlternate("recommended_graphics")]
    public string? recommendedGraphics { get; set; } = null;
    [OculusFieldAlternate("recommended_memory_gb")]
    public string? recommendedProcessor { get; set; } = null;
    [OculusFieldAlternate("recommended_processor")]
    public double? recommendedMemoryGB { get; set; } = null;
    [BsonIgnore]
    public string? recommendedMemoryGBFormatted
    {
        get
        {
            if(recommendedGraphics == null) return null;
            return SizeConverter.GigaByteSizeToString(recommendedMemoryGB.Value);
        }
    }
}