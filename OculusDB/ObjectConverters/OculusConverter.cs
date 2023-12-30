using System.Diagnostics;
using System.Globalization;
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

    public static T? AddScrapingNodeName<T>(T? toAlter, string scrapingNodeName)
    {
        if (toAlter == null) return default(T);
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
        if (oculus == null) throw new Exception("Cannot convert null object");
        IEnumerable<PropertyInfo> properties = typeof(DBType).GetProperties()
            .Where(prop => prop.IsDefined(typeof(OculusFieldAlternate), false));
        foreach (PropertyInfo property in properties)
        {
            OculusFieldAlternate oculusField = (OculusFieldAlternate)property.GetCustomAttribute(typeof(OculusFieldAlternate), false);
            // try to get value from oculus
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
        if (oculus == null) throw new Exception("Cannot convert null object");
        DBType db = new DBType();
        IEnumerable<PropertyInfo> properties = typeof(DBType).GetProperties()
            .Where(prop => prop.IsDefined(typeof(OculusField), false));
        foreach (PropertyInfo property in properties)
        {
            OculusField oculusField = (OculusField)property.GetCustomAttribute(typeof(OculusField), false);
            // try to get value from oculus
            PropertyInfo oculusProperty = typeof(OculusType).GetProperty(oculusField.fieldName);
            object? rawValue = oculusProperty.GetValue(oculus);
            property.SetValue(db, rawValue);
        }

        return db;
    }

    /// <summary>
    /// Convert OculusBinary to DBVersion
    /// </summary>
    /// <param name="detailedBinary">binary details from developer api</param>
    /// <param name="notDetailedBinary">binary details from enumeration</param>
    /// <param name="parent">parent application</param>
    /// <param name="dbApplication">parent db application</param>
    /// <param name="existingVersion">existing version from the db</param>
    /// <returns></returns>
    public static DBVersion Version(OculusBinary? detailedBinary, OculusBinary notDetailedBinary, Application parent, DBApplication dbApplication, DBVersion? existingVersion)
    {
        DBVersion version = new DBVersion();
        if (detailedBinary != null)
        {
            version = FromOculusToDB<OculusBinary, DBVersion>(detailedBinary);

            if (detailedBinary.obb_binary != null)
            {
                version.obbBinary = OBBBinary(detailedBinary.obb_binary);
            }
            version.__lastPriorityScrape = DateTime.UtcNow;
        }
        else
        {
            version.uploadedDate = notDetailedBinary.created_date_datetime;
            version.version = notDetailedBinary.version;
            version.versionCode = notDetailedBinary.versionCode;
            version.id = notDetailedBinary.id;
            version.filename = notDetailedBinary.file_name;
            Logger.Log("not detailed");
            if (existingVersion != null)
            {
                Logger.Log("existing ain't null");
                version.changelog = existingVersion.changelog;
                version.size = existingVersion.size;
                version.requiredSpace = existingVersion.requiredSpace;
                version.releaseChannels = existingVersion.releaseChannels;
                version.targetedDevices = existingVersion.targetedDevices;
                version.targetedDevicesFormatted = existingVersion.targetedDevicesFormatted;
                version.permissions = existingVersion.permissions;
                version.preDownloadEnabled = existingVersion.preDownloadEnabled;
                version.binaryStatus = existingVersion.binaryStatus;
                version.minAndroidSdkVersion = existingVersion.minAndroidSdkVersion;
                version.maxAndroidSdkVersion = existingVersion.maxAndroidSdkVersion;
                version.obbBinary = existingVersion.obbBinary;
                version.targetAndroidSdkVersion = existingVersion.targetAndroidSdkVersion;
            }
        }

        switch ((detailedBinary ?? notDetailedBinary).typename_enum)
        {
            case OculusTypeName.AndroidBinary:
                version.binaryType = HeadsetBinaryType.AndroidBinary;
                break;
            case OculusTypeName.PCBinary:
                version.binaryType = HeadsetBinaryType.PCBinary;
                break;
        }
        
        foreach (ReleaseChannel channel in (detailedBinary ?? notDetailedBinary).binary_release_channels.nodes)
        {
            version.releaseChannels.Add(new DBReleaseChannel
            {
                id = channel.id,
                name = channel.channel_name,
            });
        }
        

        if (dbApplication.group == HeadsetGroup.GoAndGearVr)
        {
            // GearVR and Go report wrong headsets as targeted devices, so let's just override them for now
            version.targetedDevices = new List<Headset> {Headset.GEARVR, Headset.PACIFIC};
        }
        version.parentApplication = ParentApplication(parent);
        return version;
    }

    public static DBObbBinary OBBBinary(AssetFile obbAsset)
    {
        DBObbBinary obbBinary = FromOculusToDB<AssetFile, DBObbBinary>(obbAsset);
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

    public static DBIapItem IAPItem(IAPItem dlc, DBApplication dbApplication)
    {
        DBIapItem db = FromOculusToDB<IAPItem, DBIapItem>(dlc);
        db.grouping = ParentApplicationGrouping(dlc.app_grouping);
        foreach (AssetFile assetFile in dlc.asset_files.nodes)
        {
            db.assetFiles.Add(AssetFile(assetFile, dbApplication));
        }
        // To get the offer id we need to get the msrp_offers->nodes[0]->id from the specific developer IAP request
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
            if(specificAchievement.unlocked_description_override_locale_map.Count > i) translation.unlockedDescription =
                specificAchievement.unlocked_description_override_locale_map[i].translation;
            translation.locale = specificAchievement.title_locale_map[i].locale;
            db.translations.Add(translation);
        }
        db.searchTitle = db.title ?? "";
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
    public static DBApplication Application(Application? applicationFromDeveloper, Application? applicationFromStore)
    {
        DBApplication db = new DBApplication();
        if (applicationFromDeveloper != null)
        {
            db = FromOculusToDB<Application, DBApplication>(applicationFromDeveloper);
            Application applicationCloudStorage = GraphQLClient.AppDetailsCloudStorageEnabled(applicationFromDeveloper.id).data.node;
            db.cloudBackupEnabled = applicationCloudStorage.cloud_backup_enabled;
            
            // Get latest public metadata revision
            PDPMetadata metadataToUse = applicationFromDeveloper.firstRevision.nodes[0].pdp_metadata;
            foreach (ApplicationRevision revision in applicationFromDeveloper.revisionsIncludingVariantMetadataRevisions.nodes)
            {
                if (revision.release_status_enum == ReleaseStatus.RELEASED)
                {
                    if (metadataToUse != null && metadataToUse.id == revision.pdp_metadata.id) break; // we already have the full metadata
                    db.hasUnpublishedMetadataInQueue = true;
                    if(revision.pdp_metadata == null) continue;
                    metadataToUse = GraphQLClient.PDPMetadata(revision.pdp_metadata.id).data.node; // fetch released metadata entry from Oculus
                    break;
                }
            }
            db = FromOculusToDBAlternate(metadataToUse, db); // populate db with info from PDPMetadata
            db.isFirstParty = metadataToUse.application.is_first_party;
            db.grouping = ApplicationGrouping(applicationFromDeveloper.grouping);
            
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
        }
        else
        {
            db = FromOculusToDB<Application, DBApplication>(applicationFromStore);
            db.cloudBackupEnabled = false;
            DBError missingStuffEror = new DBError
            {
                reason = DBErrorReason.DeveloperApplicationNull,
                type = DBErrorType.MissingOrApproximatesInformation,
                message =
                    "Some data could not be fetched by OculusDB. It was either approximated, incomplete, wrong or missing. The fields are listed below. This list may not be complete cause it's curated by ComputerElite :]",
                unknownOrApproximatedFieldsIfAny = new List<string>()
            };
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("cloudBackupEnabled");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("translations");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("shareCapabilities");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("shareCapabilitiesFormatted");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("shortDescription");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("keywords");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("genres");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("genresFormatted");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("isBlockedByVerification");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("isForOculusKeysOnly");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("isFirstParty");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("isBlockedByVerification");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("supportWebsiteUrl");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("externalSubscriptionType");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("externalSubscriptionTypeFormatted");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("supportedInAppLanguages"); // FUCK YOU OCULUS I'M NOT PARSING LANGUAGE NAMES
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("userInteractionModes"); /// I'M ALSO NOT PARSING THAT!!!
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("userInteractionModesFormatted");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("playArea");
            missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("playAreaFormatted");
            try
            {
                foreach (InputDevice device in applicationFromStore.supported_input_devices_list)
                {
                    db.supportedInputDevices.Add(device.tag_enum);
                }
            }
            catch (Exception e)
            {
                missingStuffEror.unknownOrApproximatedFieldsIfAny.Add("supportedInputDevices");
            }

            db.supportedPlayerModes = applicationFromStore.supported_player_modes_enum;
            
            db.externalSubscriptionType = ExternalSubscriptionType.UNKNOWN;
            db.category = applicationFromStore.category_enum;
            db.comfortRating = applicationFromStore.comfort_rating_enum;
            db.developerPrivacyPolicyUrl = applicationFromStore.developer_privacy_policy_url;
            db.developerTermsOfServiceUrl = applicationFromStore.developer_terms_of_service_url;
            db.websiteUrl = applicationFromStore.website_url;
            db.publisherName = applicationFromStore.publisher_name;
            db.isFirstParty = null;
            db.isForOculusKeysOnly = null;
            db.isBlockedByVerification = null;
            db.grouping = ApplicationGrouping(applicationFromStore.grouping);
            db.group = OculusPlatformToHeadsetGroup(applicationFromStore.platform_enum);
            
            try
            {
                // parse release date
                string[] split = applicationFromStore.release_info.display_name.Split(' ');
                string day = split[0];
                string monthName = split[1];
                string year = split[2];
                List<string> months = new List<string>
                {
                    "January", "February", "March", "April", "May", "June", "July", "August", "September",
                    "October", "November", "December"
                };
                int month = months.FindIndex(x => x.ToLower() == monthName.ToLower()) + 1;
                DateTime releaseDate = new DateTime(int.Parse(year), month, int.Parse(day));
                db.releaseDate = releaseDate;
                db.errors.Add(new DBError
                {
                    type = DBErrorType.ReleaseDateApproximated,
                    reason = DBErrorReason.DeveloperApplicationNull,
                    message = "Release date approximated from string"
                });
            } catch (Exception e)
            {
                db.errors.Add(new DBError
                {
                    type = DBErrorType.CouldntApproximateReleaseDate,
                    reason = DBErrorReason.DeveloperApplicationNull,
                    message = "Could not parse release date from string"
                });
            }
            
            
            // Add translation
            DBApplicationTranslation dbTranslation = new DBApplicationTranslation();
            dbTranslation.displayName = applicationFromStore.display_name;
            dbTranslation.longDescription = applicationFromStore.display_long_description;
            string trunancedDescription = applicationFromStore.display_long_description;
            if (trunancedDescription.Length > 200) trunancedDescription = trunancedDescription.Substring(0, 200);
            dbTranslation.shortDescription = trunancedDescription;
            dbTranslation.longDescriptionUsesMarkdown = applicationFromStore.long_description_uses_markdown;
            dbTranslation.keywords = null;
            db.translations.Add(dbTranslation);
        }

        db.developerName = applicationFromStore.developer_name;
        db.canonicalName = applicationFromStore.canonicalName;
        
        
        db.offerId = applicationFromStore.current_offer != null ? applicationFromStore.current_offer.id : null;

        db.searchDisplayName = db.displayName;
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
            case OculusPlatform.ANDROID:
                return HeadsetGroup.GoAndGearVr;
        }

        return HeadsetGroup.Unknown;    
    }
    
    public static DBApplicationGrouping? ApplicationGrouping(ApplicationGrouping? grouping)
    {
        if (grouping == null) return null;
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

    public static DBIapItemPack IAPItemPack(AppItemBundle dlc, DBApplicationGrouping grouping)
    {
        
        DBIapItemPack db = FromOculusToDB<AppItemBundle, DBIapItemPack>(dlc);
        if(dlc.current_offer != null) db.offerId = dlc.current_offer.id;
        db.grouping = ParentApplicationGrouping(grouping);
        foreach (Node<IAPItem> iapItem in dlc.bundle_items.edges)
        {
            db.items.Add(IAPItemChild(iapItem.node));
        }
        return db;
    }
    
    public static DBIapItemId IAPItemChild(IAPItem iapItem)
    {
        DBIapItemId db = FromOculusToDB<IAPItem, DBIapItemId>(iapItem);
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