// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.FunctionalTests.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.IdentityModel.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class JwtTokenExtractorTests
    {
        private const string KeyId = "CtfQC8Le-8NsC7oC2zQkZpcrfOc";
        private const string TestChannelName = "testChannel";
        private const string ComplianceEndorsement = "o365Compliant";
        private const string RandomEndorsement = "2112121212";

        private readonly HttpClient client;
        private readonly HttpClient emptyClient;

        public JwtTokenExtractorTests()
        {
            ChannelValidation.ToBotFromChannelTokenValidationParameters.ValidateLifetime = false;
            ChannelValidation.ToBotFromChannelTokenValidationParameters.ValidateIssuer = false;

            client = new HttpClient
            {
                BaseAddress = new Uri("https://webchat.botframework.com/"),
            };
            emptyClient = new HttpClient();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Connector_TokenExtractor_NullRequiredEndorsements_ShouldFail()
        {
            var configRetriever = new TestConfigurationRetriever();

            configRetriever.EndorsementTable.Add(KeyId, new HashSet<string>() { RandomEndorsement, ComplianceEndorsement, TestChannelName });
            await RunTestCase(configRetriever);
        }

        [TestMethod]
        public async Task Connector_TokenExtractor_EmptyRequireEndorsements_ShouldValidate()
        {
            var configRetriever = new TestConfigurationRetriever();

            configRetriever.EndorsementTable.Add(KeyId, new HashSet<string>() { RandomEndorsement, ComplianceEndorsement, TestChannelName });
            var claimsIdentity = await RunTestCase(configRetriever, new string[] { });
            Assert.IsTrue(claimsIdentity.IsAuthenticated);
        }

        [TestMethod]
        public async Task Connector_TokenExtractor_RequiredEndorsementsPresent_ShouldValidate()
        {
            var configRetriever = new TestConfigurationRetriever();

            configRetriever.EndorsementTable.Add(KeyId, new HashSet<string>() { RandomEndorsement, ComplianceEndorsement, TestChannelName });
            var claimsIdentity = await RunTestCase(configRetriever, new string[] { ComplianceEndorsement });
            Assert.IsTrue(claimsIdentity.IsAuthenticated);
        }

        private async Task<ClaimsIdentity> RunTestCase(IConfigurationRetriever<IDictionary<string, HashSet<string>>> configRetriever, string[] requiredEndorsements = null)
        {
            var tokenExtractor = new JwtTokenExtractor(
                emptyClient,
                EmulatorValidation.ToBotFromEmulatorTokenValidationParameters,
                AuthenticationConstants.ToBotFromEmulatorOpenIdMetadataUrl,
                AuthenticationConstants.AllowedSigningAlgorithms,
                new ConfigurationManager<IDictionary<string, HashSet<string>>>("http://test", configRetriever));

            string header = $"Bearer {await new MicrosoftAppCredentials(EnvironmentConfig.TestAppId(), EnvironmentConfig.TestAppPassword()).GetTokenAsync()}";

            return await tokenExtractor.GetIdentityAsync(header, "testChannel", requiredEndorsements);
        }
    }
}
