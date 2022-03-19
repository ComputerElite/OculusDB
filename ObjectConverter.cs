using ComputerUtils.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB
{
    public class ObjectConverter
    {
        public static NewMom Convert<NewMom, YourMom> (YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach(FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }

        public static NewMom ConvertCopy<NewMom, YourMom>(YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if(typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null) typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }

        public static NewMom Convert<NewMom, YourMom, HisMom>(YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            foreach (FieldInfo father in typeof(HisMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                father.SetValue(converted, father.GetValue(toConvert));
            }
            return converted;
        }

        public static dynamic ConvertToDBType(BsonDocument d)
        {
            if (d == null) return null;
            string type = d.GetValue("__OculusDBType").ToString();
            if(type == DBDataTypes.Application)
            {
                return BsonSerializer.Deserialize<DBApplication>(d);
            }
            else if (type == DBDataTypes.IAPItem)
            {
                return BsonSerializer.Deserialize<DBIAPItem>(d);
            }
            else if (type == DBDataTypes.IAPItemPack)
            {
                return BsonSerializer.Deserialize<DBIAPItemPack>(d);
            }
            else if (type == DBDataTypes.Version)
            {
                return BsonSerializer.Deserialize<DBVersion>(d);
            }
            else if (type == DBDataTypes.ActivityNewApplication)
            {
                return BsonSerializer.Deserialize<DBActivityNewApplication>(d);
            }
            else if (type == DBDataTypes.ActivityNewVersion)
            {
                return BsonSerializer.Deserialize<DBActivityNewVersion>(d);
            }
            else if (type == DBDataTypes.ActivityVersionUpdated)
            {
                return BsonSerializer.Deserialize<DBActivityVersionUpdated>(d);
            }
            else if (type == DBDataTypes.ActivityPriceChanged)
            {
                return BsonSerializer.Deserialize<DBActivityPriceChanged>(d);
            }
            else if (type == DBDataTypes.ActivityNewDLC)
            {
                return BsonSerializer.Deserialize<DBActivityNewDLC>(d);
            }
            else if (type == DBDataTypes.ActivityNewDLCPack)
            {
                return BsonSerializer.Deserialize<DBActivityNewDLCPack>(d);
            }
            else if (type == DBDataTypes.ActivityDLCUpdated)
            {
                return BsonSerializer.Deserialize<DBActivityDLCUpdated>(d);
            }
            else if (type == DBDataTypes.ActivityDLCPackUpdated)
            {
                return BsonSerializer.Deserialize<DBActivityDLCPackUpdated>(d);
            }
            return d;
        }
    }
}
