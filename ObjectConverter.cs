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
            return d;
        }
    }
}
