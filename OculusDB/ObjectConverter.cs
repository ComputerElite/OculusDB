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
using OculusDB.ObjectConverters;

namespace OculusDB
{
    public class ObjectConverterIgnore : System.Attribute
    {

    }
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
                try
                {
                    if (typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null) typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).SetValue(converted, father.GetValue(toConvert));
                }
                catch { }
            }
            return converted;
        }

        public static NewMom ConvertCopy<NewMom, YourMom, HisMom>(YourMom toConvert) where NewMom : new()
        {
            NewMom converted = new NewMom();
            foreach (FieldInfo father in typeof(YourMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) != null && Attribute.IsDefined(typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), typeof(ObjectConverterIgnore))) typeof(NewMom).GetField(father.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).SetValue(converted, father.GetValue(toConvert));
            }
            foreach (FieldInfo father in typeof(HisMom).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
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

        public static dynamic? ConvertToDBType(DBBase o)
        {
            switch (o.__OculusDBType)
            {
                case DBDataTypes.Application:
                    return (DBApplication)o;
                case DBDataTypes.Version:
                    return (DBVersion)o;
                case DBDataTypes.IapItem:
                    return (DBIAPItem)o;
                case DBDataTypes.IapItemPack:
                    return (DBIAPItemPack)o;
                case DBDataTypes.AppImage:
                    return (DBAppImage)o;
                case DBDataTypes.Achievement:
                    return (DBAchievement)o;
                case DBDataTypes.Offer:
                    return (DBOffer)o;
                case DBDataTypes.Difference:
                    return (DBDifference)o;
            }
            return o;
        }
    }
}
