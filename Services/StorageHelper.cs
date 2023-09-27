using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiplomskiChatBot.Services
{
    public class StorageHelper : IStorageHelper
    {
        private readonly IConfiguration _configuration;

        public StorageHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<TableClient> GetTableServiceClient(string table)
        {
            TableServiceClient tableServiceClient = new TableServiceClient(_configuration.GetConnectionString("AzureStorage"));
            TableClient tableClient = tableServiceClient.GetTableClient(tableName: table);

            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }

        public async Task UpsertEntityAsync<T>(string table, T entity) where T : ITableEntity
        {
            try
            {
                TableClient tableClient = await GetTableServiceClient(table);
                await tableClient.UpsertEntityAsync(entity);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while upserting entity:" + ex.Message);
            }
        }

        public async Task<T> GetEntityAsync<T>(string table, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                TableClient tableClient = await GetTableServiceClient(table);
                return await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while getting entity:" + ex.Message);
                return null;
            }
        }

        public async Task DeleteEntityAsync(string table, string partitionKey, string rowKey)
        {
            try
            {
                TableClient tableClient = await GetTableServiceClient(table);
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while deleting entity:" + ex.Message);
            }
        }
    }
}