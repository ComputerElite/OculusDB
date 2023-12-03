using System.Reflection;
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
                Logger.Log("setting " + sn.Name);
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
    
    public static DBParentApplication ParentApplication(Application application)
    {
        DBParentApplication parentApplication = new DBParentApplication();
        parentApplication.id = application.id;
        parentApplication.displayName = application.display_name;
        return parentApplication;
    }
    
    public static DBParentApplication ParentApplication(DBApplication application)
    {
        DBParentApplication parentApplication = new DBParentApplication();
        parentApplication.id = application.id;
        parentApplication.displayName = application.displayName;
        return parentApplication;
    }
    
    /// <summary>
    /// Fetches full metadata for application entry if first is not RELEASED
    /// </summary>
    /// <param name="application"></param>
    /// <returns></returns>
    public static DBApplication Application(Application application)
    {
        DBApplication db = FromOculusToDB<Application, DBApplication>(application);
        
        // Get latest public metadata revision
        PDPMetadata metadataToUse = application.firstRevision.nodes[0].pdp_metadata;
        foreach (ApplicationRevision revision in application.revisionsIncludingVariantMetadataRevisions.nodes)
        {
            if (revision.release_status_enum == ReleaseStatus.RELEASED)
            {
                if (metadataToUse.id == revision.pdp_metadata.id) break; // we already have the full metadata
                metadataToUse = GraphQLClient.PDPMetadata(revision.pdp_metadata.id).data.node; // fetch released metadata entry from Oculus
                break;
            }
        }
        db = FromOculusToDBAlternate(metadataToUse, db); // populate db with info from PDPMetadata
        db.isFirstParty = metadataToUse.application.is_first_party;
        
        db.grouping = ApplicationGrouping(application.grouping);
        
        db.offerId = application.baseline_offer != null ? application.baseline_offer.id : null;
        
        // Get share capabilities
        Application? shareCapabilitiesApplication = GraphQLClient.GetAppSharingCapabilities(application.id).data.node;
        db.shareCapabilities = shareCapabilitiesApplication.share_capabilities_enum;
        
        // Set application group
        switch (application.platform_enum)
        {
            case OculusPlatform.PC:
                db.group = HeadsetGroup.PCVR;
                break;
            case OculusPlatform.ANDROID_6DOF:
                db.group = HeadsetGroup.Quest;
                break;
        }
        
        // Add translations
        db.defaultLocale = metadataToUse.application.default_locale;
        foreach (ApplicationTranslation translation in metadataToUse.translations.nodes)
        {
            foreach (OculusImage img in translation.imagesExcludingScreenshotsAndMarkdown.nodes)
            {
                if(img.image_type_enum == ImageType.APP_IMG_COVER_SQUARE) db.oculusImageUrl = img.uri;    
            }
            DBApplicationTranslation dbTranslation = FromOculusToDB<ApplicationTranslation, DBApplicationTranslation>(translation);
            dbTranslation.parentApplication = ParentApplication(application);
            db.translations.Add(dbTranslation);
        }
        return db;
    }
    
    public static DBApplicationGrouping ApplicationGrouping(ApplicationGrouping grouping)
    {
        DBApplicationGrouping db = FromOculusToDB<ApplicationGrouping, DBApplicationGrouping>(grouping);
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
}