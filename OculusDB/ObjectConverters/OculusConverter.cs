using System.Reflection;
using ComputerUtils.Logging;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.ObjectConverters;

public class OculusConverter
{
    public static DBType FromOculusToDBAlternate<OculusType, DBType>(OculusType oculus, DBType toPopulate) where DBType : new()
    {
        IEnumerable<PropertyInfo> properties = typeof(DBType).GetProperties()
            .Where(prop => prop.IsDefined(typeof(OculusFieldAlternate), false));
        foreach (PropertyInfo property in properties)
        {
            OculusFieldAlternate oculusField = (OculusFieldAlternate)property.GetCustomAttribute(typeof(OculusFieldAlternate), false);
            // try to get value from oculus
            PropertyInfo oculusProperty = typeof(OculusType).GetProperty(oculusField.fieldName);
            Logger.Log(oculusField.fieldName);
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
            object? value = Convert.ChangeType(oculusProperty.GetValue(oculus), property.PropertyType);
            property.SetValue(db, value);
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
        db.defaultLocale = metadataToUse.application.default_locale;
        switch (application.platform_enum)
        {
            case OculusPlatform.PC:
                db.group = HeadsetGroup.PCVR;
                break;
            case OculusPlatform.ANDROID_6DOF:
                db.group = HeadsetGroup.Quest;
                break;
        }
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