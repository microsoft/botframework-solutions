using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json.Linq;
using CalendarSkill.Models;

namespace CalendarSkill.Services
{
    public class AzureCosmosService
    {
        private readonly string _authKey;
        private readonly Uri _cosmosEndpoint;
        private readonly string _databaseId;
        private readonly string _partitionKey;
        private readonly string _collectionId;
        private readonly IDocumentClient _client;
        private string _collectionLink = null;

        public AzureCosmosService(BotSettings settings)
        {
            _authKey = settings.CosmosDb.AuthKey;
            _cosmosEndpoint = settings.CosmosDb.CosmosDBEndpoint;
            _databaseId = settings.AzureSearch.SourceDbName;
            _collectionId = settings.AzureSearch.SourceCollectionName;
            _partitionKey = "";
            _client = new DocumentClient(_cosmosEndpoint, _authKey);

        }

        public bool DataExpiredOrNonExist()
        {
            return true;
        }

        public async Task<bool> UpdateAsync(List<PlaceModel> changes)
        {
            await _client.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseId }).ConfigureAwait(false);

            var documentCollection = new DocumentCollection
            {
                Id = _collectionId,
            };

            if (_collectionLink == null)
            {
                var response = await _client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(_databaseId),
                documentCollection)
                .ConfigureAwait(false);

                _collectionLink = response.Resource.SelfLink;
            }

            foreach (var change in changes)
            {
                var doc = await _client.UpsertDocumentAsync(_collectionLink, change, disableAutomaticIdGeneration: true);
            }

            return true;
        }
    }
}
