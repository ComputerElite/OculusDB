using System.Linq.Expressions;
using MongoDB.Driver;

namespace OculusDB.Database;

public interface IDBObjectOperations<T>
{
    public T GetEntryForDiffGeneration(IMongoCollection<T> collection);
    public void AddOrUpdateEntry(IMongoCollection<T> collection);
}