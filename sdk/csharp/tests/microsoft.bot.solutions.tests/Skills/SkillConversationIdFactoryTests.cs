using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Solutions.Tests.Skills
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class SkillConversationIdFactoryTests
    {
        [TestMethod]
        public void SkillConversationIdFactoryNullStorageTest()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new SkillConversationIdFactory(null));
            Assert.IsTrue(ex.Message.Contains("Value cannot be null. (Parameter 'storage')"));
        }

        [TestMethod]
        [ExpectedException(
            typeof(ArgumentNullException),
            "Value cannot be null.\r\nParameter name: options")]
        public async Task CreateSkillConversationIdNullOptionsTest()
        {
            var storage = new MemoryStorage();
            var skillConversationIdFactory = new SkillConversationIdFactory(storage);
            await skillConversationIdFactory.CreateSkillConversationIdAsync(options: null, CancellationToken.None);
        }

        [TestMethod]
        [ExpectedException(
    typeof(ArgumentNullException),
    "Value cannot be null.\r\nParameter name: skillConversationId")]
        public async Task GetSkillConversationReferenceAsyncNullConversationIdTest()
        {
            var storage = new MemoryStorage();
            var skillConversationIdFactory = new SkillConversationIdFactory(storage);
            await skillConversationIdFactory.GetSkillConversationReferenceAsync(null, CancellationToken.None);
        }

        [TestMethod]
        public async Task GetSkillConversationReferenceAsyncTest()
        {
            var storage = new MemoryStorage();
            JObject skillConversationReferenceJObject = new JObject
            {
                ["Test"] = JsonConvert.SerializeObject(new SkillConversationReference
                {
                    ConversationReference = new ConversationReference
                    {
                        Conversation = new ConversationAccount
                        {
                            Id = "test",
                            Name = "Test",
                            ConversationType = "test",
                            TenantId = "Test",
                        },
                    },
                    OAuthScope = "Test",
                }),
            };

            var skillConversationInfo = new Dictionary<string, object>
            {
                {
                    "Test",
                    skillConversationReferenceJObject
                },
            };
            await storage.WriteAsync(skillConversationInfo, CancellationToken.None).ConfigureAwait(false);
            var skillConversationIdFactory = new SkillConversationIdFactory(storage);
            var skillConversationRef = await skillConversationIdFactory.GetSkillConversationReferenceAsync("Test", CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(skillConversationRef);
        }

        [TestMethod]
        public async Task DeleteConversationReferenceAsyncTest()
        {
            var storage = new MemoryStorage();
            JObject skillConversationReferenceJObject = new JObject
            {
                ["Test"] = JsonConvert.SerializeObject(new SkillConversationReference
                {
                    ConversationReference = new ConversationReference
                    {
                        Conversation = new ConversationAccount
                        {
                            Id = "test",
                            Name = "Test",
                            ConversationType = "test",
                            TenantId = "Test",
                        },
                    },
                    OAuthScope = "Test",
                }),
            };

            var skillConversationInfo = new Dictionary<string, object>
            {
                {
                    "Test",
                    skillConversationReferenceJObject
                },
            };
            await storage.WriteAsync(skillConversationInfo, CancellationToken.None).ConfigureAwait(false);
            var skillConversationIdFactory = new SkillConversationIdFactory(storage);
            await skillConversationIdFactory.DeleteConversationReferenceAsync("Test", CancellationToken.None).ConfigureAwait(false);
            var skillConversationRef = await skillConversationIdFactory.GetSkillConversationReferenceAsync("Test", CancellationToken.None).ConfigureAwait(false);
            Assert.IsNull(skillConversationRef);
        }

        [TestMethod]
        public async Task CreateSkillConversationIdAsyncTest()
        {
            var storage = new MemoryStorage();
            var skillConversationIdFactory = new SkillConversationIdFactory(storage);

            var activity = new Activity
            {
                Conversation = new ConversationAccount
                {
                    Id = "test",
                    Name = "Test",
                    ConversationType = "test",
                    TenantId = "Test",
                },
                ChannelId = "test",
                Id = Guid.NewGuid().ToString(),
            };

            var options = new SkillConversationIdFactoryOptions
            {
                FromBotOAuthScope = "Test",
                FromBotId = "Test",
                Activity = activity,
                BotFrameworkSkill = new BotFrameworkSkill { Id = "test", AppId = "test", SkillEndpoint = new Uri("http://test.com") },
            };
            var storageKey = await skillConversationIdFactory.CreateSkillConversationIdAsync(options, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(storageKey);
            Assert.AreEqual("test-test-test-skillconvo", storageKey);
        }
    }
}
