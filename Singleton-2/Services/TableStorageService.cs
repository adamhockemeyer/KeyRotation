using Entities;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Singleton_2.Services
{
    public class TableStorageService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TableStorageService> _logger;
        private readonly AsyncRetryPolicy _handleAuthenticationFailurePolicy;
        private CloudTableClient _tableClient;
        private StorageCredentials _accountSas;

        public TableStorageService(IConfiguration config, ILogger<TableStorageService> logger)
        {
            _config = config;
            _logger = logger;


            // We want to be able to auto-refresh our clients SAS Token when 'StorageException: Server failed to authenticate the request.' occurs.
            _handleAuthenticationFailurePolicy = Policy
                                                    .Handle<StorageException>(p => p.Message.Contains("Server failed to authenticate the request."))
                                                    .WaitAndRetryAsync(
                                                        3,
                                                        retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                                                        (exception, timeSpan, retryCount, context) =>
                                                        {
                                                            logger?.LogInformation("[StorageException Retry] Retry attempt wait {retryAttemptTime}.  Max attempts {maxRetryAttempts}", timeSpan, retryCount);

                                                            // Update storage account connection.
                                                            InitCloudTableClient(true);
                                                        });

            InitCloudTableClient();
        }

        /// <summary>
        /// Initialize the CloudTableClient for use by the TableQueries.
        /// </summary>
        /// <param name="forceConfigRefresh">Updates configuration providers to handle key/sas token changes due to rotation.</param>
        private void InitCloudTableClient(bool forceConfigRefresh = false)
        {
            if(forceConfigRefresh)
            {
                // Reload the configuration from Azure KeyVault.
                ((IConfigurationRoot)_config).Reload();
            }

            var accountName = _config["Azure:Storage:AccountName"];
            var sasToken = _config[$"{accountName}-{_config["Azure:Storage:SasDefinition"]}"];

            if (_tableClient == null)
            {
                _accountSas = new StorageCredentials(sasToken);
                var storageAccount = new CloudStorageAccount(_accountSas, accountName, null, true);
                _tableClient = storageAccount.CreateCloudTableClient();
            }

            if (forceConfigRefresh)
            {
                // Update the SASToken with the new value from Reload()
                _accountSas.UpdateSASToken(sasToken);
            }
        }

        /// <summary>
        /// Return all Entities from the given table.
        /// </summary>
        /// <typeparam name="T">Entity of type <see cref="ITableEntity"/></typeparam>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAll<T>(string tableName) where T: ITableEntity, new()
        {
            var table = _tableClient.GetTableReference(tableName);

            return await _handleAuthenticationFailurePolicy.ExecuteAsync(() => ExecuteQueryAsync<T>(table, new TableQuery<T>()));
        }

        private static async System.Threading.Tasks.Task<IEnumerable<DynamicTableEntity>> ExecuteQueryAsync(CloudTable table, TableQuery query)
        {
            TableContinuationToken token = null;
            var retVal = new List<DynamicTableEntity>();
            do
            {
                var results = await table.ExecuteQuerySegmentedAsync(query, token);
                retVal.AddRange(results.Results);
                token = results.ContinuationToken;
            } while (token != null);

            return retVal;
        }

        private static async System.Threading.Tasks.Task<IEnumerable<T>> ExecuteQueryAsync<T>(CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
        {
            TableContinuationToken token = null;
            var retVal = new List<T>();
            do
            {
                var results = await table.ExecuteQuerySegmentedAsync(query, token);
                retVal.AddRange(results.Results);
                token = results.ContinuationToken;
            } while (token != null);

            return retVal;
        }
    }


}
