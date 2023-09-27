using Azure.Data.Tables;
using System.Threading.Tasks;

namespace DiplomskiChatBot.Services
{
    public interface IStorageHelper
    {
        Task UpsertEntityAsync<T>(string table, T entity) where T : ITableEntity;

        Task<T> GetEntityAsync<T>(string table, string partitionKey, string rowKey) where T : class, ITableEntity, new();

        Task DeleteEntityAsync(string table, string partitionKey, string rowKey);
    }
}