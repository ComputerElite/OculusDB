using System.Linq.Expressions;
using MongoDB.Driver;

namespace OculusDB.Database;

public interface IDBObjectOperations<T>
{
    public T? GetEntryForDiffGeneration(IEnumerable<T> collection);
    public void AddOrUpdateEntry(IMongoCollection<T> collection);
}