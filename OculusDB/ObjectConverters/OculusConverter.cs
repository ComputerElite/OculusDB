using System.Reflection;
using System.Security.AccessControl;
using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.ObjectConverters;

public class OculusConverter
{

    public static T AddScrapingNodeName<T>(T toAlter, string scrapingNodeName)
    {
        List<PropertyInfo> toSet = typeof(T).GetProperties().Where(x => x.Name == "__sn").ToList();
        if (toSet.Count >= 1)
        {
            foreach (PropertyInfo sn in toSet)
            {
                sn.SetValue(toAlter, scrapingNodeName);
            }
        }
        
        // Recursively go through all fields which could have __sn
        // the objects
        IEnumerable<PropertyInfo> properties = typeof(T).GetProperties()
            .Where(prop => prop.IsDefined(typeof(ObjectScrapingNodeFieldPresent), false));
        MethodInfo addScrapingNodeNameMethodInfo = typeof(OculusConverter).GetMethod("AddScrapingNodeName");
        foreach (PropertyInfo property in properties)
        {
            
            object childObject = property.GetValue(toAlter);
            if(childObject == null) continue;
            
            object convertedValue = addScrapingNodeNameMethodInfo.MakeGenericMethod(property.PropertyType).Invoke(null, new object[] {childObject, scrapingNodeName});
            property.SetValue(toAlter, convertedValue);
        }
        
        
        // Recursively go through all fields which could have __sn
        // And the lists
        properties = typeof(T).GetProperties()
            .Where(prop => prop.IsDefined(typeof(ListScrapingNodeFieldPresent), false));
        foreach (PropertyInfo property in properties)
        {
            object list = property.GetValue(toAlter);
            if(list == null) continue;
            
            Type elementType = property.PropertyType.GetGenericArguments()[0];
            // Use reflection to get the Count property and iterate through the list
            PropertyInfo countProperty = property.PropertyType.GetProperty("Count");
            int count = (int)countProperty.GetValue(list);

            // Iterate through the list elements
            for (int i = 0; i < count; i++)
            {
                object listItem = property.PropertyType.GetProperty("Item").GetValue(list, new object[] { i });
                object convertedValue = addScrapingNodeNameMethodInfo.MakeGenericMethod(elementType).Invoke(null, new object[] {listItem, scrapingNodeName});
                property.PropertyType.GetProperty("Item").SetValue(list, convertedValue, new object[] { i });
            }
        }

        return toAlter;
    }

    public static DBType FromOculusToDBAlternate<OculusType, DBType>(OculusType oculus, DBType toPopulate) where DBType : new()
    {
        IEnumerable<PropertyInfo> properties = typeof(DBType).GetProperties()
            .Where(prop => prop.IsDefined(typeof(OculusFieldAlternate), false));
        foreach (PropertyInfo property in properties)
        {
            OculusFieldAlternate oculusField = (OculusFieldAlternate)property.GetCustomAttribute(typeof(OculusFieldAlternate), false);
            // try to get value from oculus
            Logger.Log(oculusField.fieldName);
            PropertyInfo oculusProperty = typeof(OculusType).GetProperty(oculusField.fieldName);
            object? rawValue = oculusProperty.GetValue(oculus);
            property.SetValue(toPopulate, rawValue);
        }

        return toPopulate;
    }

    public static DBType FromOculusToDBAlternate<OculusType, DBType>(OculusType oculus) where DBType : new()
    {
        return FromOculusToDBAlternate<OculusType, DBType>(oculus, new DBType());
    }
    public static DBType FromOculusToDB<OculusType, DBType>(OculusType oculus) where DBType : new()
    {
        DBType db = new DBType();
        IEnumerable<PropertyInfo> properties = typeof(DBType).GetProperties()
            .Where(prop => prop.IsDefined(typeof(OculusField), false));
        foreach (PropertyInfo property in properties)
        {
            OculusField oculusField = (OculusField)property.GetCustomAttribute(typeof(OculusField), false);
            // try to get value from oculus
            PropertyInfo oculusProperty = typeof(OculusType).GetProperty(oculusField.fieldName);
            Logger.Log(oculusField.fieldName);
            object? rawValue = oculusProperty.GetValue(oculus);
            property.SetValue(db, rawValue);
        }

        return db;
    }

    public static DBVersion Version(OculusBinary binary, Application parent, List<VersionAlias> appAliases)
    {
        DBVersion version = FromOculusToDB<OculusBinary, DBVersion>(binary);
        version.parentApplication = ParentApplication(parent);
        foreach (ReleaseChannel channel in binary.binary_release_channels.nodes)
        {
            version.releaseChannels.Add(new DBReleaseChannel
            {
                id = channel.id,
                name = channel.channel_name,
            });
        }

        if (binary.obb_binary != null)
        {
            version.obbBinary = OBBBinary(binary.obb_binary);
        }

        switch (binary.typename_enum)
        {
            case OculusTypeName.AndroidBinary:
                version.binaryType = HeadsetBinaryType.AndroidBinary;
                break;
            case OculusTypeName.PCBinary:
                version.binaryType = HeadsetBinaryType.PCBinary;
                break;
        }
        version.alias = appAliases.Find(a => a.versionId == version.id)?.alias;
        return version;
    }

    public static DBOBBBinary OBBBinary(AssetFile obbAsset)
    {
        DBOBBBinary obbBinary = FromOculusToDB<AssetFile, DBOBBBinary>(obbAsset);
        return obbBinary;
    }

    public static DBParentApplication ParentApplication(Application application)
    {
        DBParentApplication parentApplication = new DBParentApplication();
        parentApplication.id = application.id;
        parentApplication.displayName = application.display_name;
        return parentApplication;
    }
    
    public static DBParentApplication ParentApplication(BinaryApplication application, DBApplication dbApp)
    {
        DBParentApplication parentApplication = new DBParentApplication();
        parentApplication.id = application.id;
        parentApplication.displayName = dbApp.displayName;
        return parentApplication;
    }
    
    public static DBParentApplication ParentApplication(DBApplication application)
    {
        DBParentApplication parentApplication = new DBParentApplication();
        parentApplication.id = application.id;
        parentApplication.displayName = application.displayName;
        return parentApplication;
    }

    public static DBIAPItem IAPItem(IAPItem dlc, DBApplication dbApplication)
    {
        DBIAPItem db = FromOculusToDB<IAPItem, DBIAPItem>(dlc);
        db.grouping = ParentApplicationGrouping(dlc.app_grouping);
        foreach (AssetFile assetFile in dlc.asset_files.nodes)
        {
            db.assetFiles.Add(AssetFile(assetFile, dbApplication));
        }
        // To get the offer id we need to get the msrp_offers->nodes[0]->id from the specific developer IAP request
        Logger.Log(JsonSerializer.Serialize(GraphQLClient.GetAddOnDeveloper(db.id, dbApplication.id)));
        IAPItem specificDlc = GraphQLClient.GetAddOnDeveloper(db.id, dbApplication.id).data.node?.grouping.add_ons.nodes[0];
        db = FromOculusToDBAlternate(specificDlc, db);
        if (specificDlc != null && specificDlc.msrp_offers.nodes.Count > 0)
        {
            db.offerId = specificDlc.msrp_offers.nodes[0].id;
        }
        return db;
    }

    public static DBOffer? Price(AppStoreOffer? offer, DBApplication parentApplication)
    {
        if (offer == null) return null;
        DBOffer db = new DBOffer();
        db.id = offer.id;
        db.parentApplication = ParentApplication(parentApplication);
        if (offer.price != null)
        {
            DBPrice price = new DBPrice();
            price.currency = offer.price.currency;
            price.price = offer.price.offset_amount_numerical;
            price.priceFormatted = offer.price.formatted;
            db.price = price;
            db.currency = offer.price.currency;
        }
        if (offer.strikethrough_price != null)
        {
            DBPrice strikethroughPrice = new DBPrice();
            strikethroughPrice.currency = offer.strikethrough_price.currency;
            strikethroughPrice.price = offer.strikethrough_price.offset_amount_numerical;
            strikethroughPrice.priceFormatted = offer.strikethrough_price.formatted;
            db.strikethroughPrice = strikethroughPrice;
        }
        db.currency = offer.price.currency;
        return db;
    }
    
    public static DBAchievement Achievement(AchievementDefinition achievement, DBApplication dbApplication)
    {
        DBAchievement db = FromOculusToDB<AchievementDefinition, DBAchievement>(achievement);
        AchievementDefinition specificAchievement = GraphQLClient.GetAchievement(db.id).data.node;
        db = FromOculusToDBAlternate(specificAchievement, db);
        db.grouping = ParentApplicationGrouping(specificAchievement.application_grouping);
        for (int i = 0; i < specificAchievement.title_locale_map.Count; i++)
        {
            DBAchievementTranslation translation = new DBAchievementTranslation();
            translation.title = specificAchievement.title_locale_map[i].translation;
            translation.description = specificAchievement.description_locale_map[i].translation;
            translation.unlockedDescription =
                specificAchievement.unlocked_description_override_locale_map[i].translation;
            translation.locale = specificAchievement.title_locale_map[i].locale;
            db.translations.Add(translation);
        }
        return db;
    }

    public static DBAssetFile AssetFile(AssetFile assetFile, DBApplication dbApplication)
    {
        DBAssetFile db = FromOculusToDB<AssetFile, DBAssetFile>(assetFile);
        db.parentApplication = ParentApplication(assetFile.binary_application, dbApplication);
        db.group = OculusPlatformToHeadsetGroup(assetFile.platform_enum);
        return db;
    }
    
    /// <summary>
    /// Fetches full metadata for application entry if first is not RELEASED
    /// </summary>
    /// <param name="application"></param>
    /// <returns></returns>
    public static DBApplication Application(Application applicationFromDeveloper, Application applicationFromStore)
    {
        DBApplication db = FromOculusToDB<Application, DBApplication>(applicationFromDeveloper);
        
        // Get latest public metadata revision
        PDPMetadata metadataToUse = applicationFromDeveloper.firstRevision.nodes[0].pdp_metadata;
        foreach (ApplicationRevision revision in applicationFromDeveloper.revisionsIncludingVariantMetadataRevisions.nodes)
        {
            if (revision.release_status_enum == ReleaseStatus.RELEASED)
            {
                if (metadataToUse.id == revision.pdp_metadata.id) break; // we already have the full metadata
                db.hasUnpublishedMetadataInQueue = true;
                metadataToUse = GraphQLClient.PDPMetadata(revision.pdp_metadata.id).data.node; // fetch released metadata entry from Oculus
                break;
            }
        }
        db = FromOculusToDBAlternate(metadataToUse, db); // populate db with info from PDPMetadata
        db.isFirstParty = metadataToUse.application.is_first_party;
        db.canonicalName = applicationFromStore.canonicalName;
        
        db.grouping = ApplicationGrouping(applicationFromDeveloper.grouping);
        
        db.offerId = applicationFromStore.current_offer != null ? applicationFromStore.current_offer.id : null;
        
        // Get share capabilities
        Application? shareCapabilitiesApplication = GraphQLClient.GetAppSharingCapabilities(applicationFromDeveloper.id).data.node;
        db.shareCapabilities = shareCapabilitiesApplication.share_capabilities_enum;

        db.group = OculusPlatformToHeadsetGroup(applicationFromDeveloper.platform_enum);
        
        // Add translations
        db.defaultLocale = metadataToUse.application.default_locale;
        foreach (ApplicationTranslation translation in metadataToUse.translations.nodes)
        {
            foreach (OculusImage img in translation.imagesExcludingScreenshotsAndMarkdown.nodes)
            {
                if(img.image_type_enum == ImageType.APP_IMG_COVER_SQUARE) db.oculusImageUrl = img.uri;    
            }
            DBApplicationTranslation dbTranslation = FromOculusToDB<ApplicationTranslation, DBApplicationTranslation>(translation);
            dbTranslation.parentApplication = ParentApplication(applicationFromDeveloper);
            db.translations.Add(dbTranslation);
        }
        return db;
    }

    public static HeadsetGroup OculusPlatformToHeadsetGroup(OculusPlatform platform)
    {
        // Set application group
        switch (platform)
        {
            case OculusPlatform.PC:
                return HeadsetGroup.PCVR;
            case OculusPlatform.ANDROID_6DOF:
                return HeadsetGroup.Quest;
        }

        return HeadsetGroup.Unknown;
    }
    
    public static DBApplicationGrouping ApplicationGrouping(ApplicationGrouping grouping)
    {
        DBApplicationGrouping db = FromOculusToDB<ApplicationGrouping, DBApplicationGrouping>(grouping);
        return db;
    }
    public static DBParentApplicationGrouping ParentApplicationGrouping(ApplicationGrouping grouping)
    {
        DBParentApplicationGrouping db = FromOculusToDB<ApplicationGrouping, DBParentApplicationGrouping>(grouping);
        return db;
    }
    public static DBParentApplicationGrouping ParentApplicationGrouping(DBApplicationGrouping grouping)
    {
        DBParentApplicationGrouping db = new DBParentApplicationGrouping();
        db.id = grouping.id;
        return db;
    }

    public static string FormatOculusEnumString(string enumString)
    {
        List<string> words = enumString.Split('_').ToList();
        for(int i = 0; i < words.Count; i++)
        {
            words[i] = words[i].ToLower();
        }
        words[0] = words[0][0].ToString().ToUpper() + words[0].Substring(1); // capitalize first letter
        return String.Join(' ', words);
    }

    public static DBIAPItemPack IAPItemPack(AppItemBundle dlc, DBApplicationGrouping grouping)
    {
        
        DBIAPItemPack db = FromOculusToDB<AppItemBundle, DBIAPItemPack>(dlc);
        if(dlc.current_offer != null) db.offerId = dlc.current_offer.id;
        db.grouping = ParentApplicationGrouping(grouping);
        foreach (Node<IAPItem> iapItem in dlc.bundle_items.edges)
        {
            db.items.Add(IAPItemChild(iapItem.node));
        }
        return db;
    }
    
    public static DBIAPItemId IAPItemChild(IAPItem iapItem)
    {
        DBIAPItemId db = FromOculusToDB<IAPItem, DBIAPItemId>(iapItem);
        return db;
    }

    public static string FormatDBEnumString(string toString)
    {
        // split by capital letters
        List<string> words = new List<string>();
        string currentWord = "";
        foreach (char c in toString)
        {
            if (Char.IsUpper(c))
            {
                if (currentWord.Length > 0) words.Add(currentWord);
                currentWord = c.ToString();
            }
            else
            {
                currentWord += c;
            }
        }
        if (currentWord.Length > 0) words.Add(currentWord);
        return String.Join(' ', words);
    }
}