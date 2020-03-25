namespace ITSMSkill.Proactive
{
    using Microsoft.Bot.Builder;

    public class ServiceNowProactiveState : BotState
    {
        /// <summary>The key used to cache the state information in the turn context.</summary>
        private const string StorageKey = "ProactiveState";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceNowProactiveState"/> class.</summary>
        /// <param name="storage">The storage provider to use.</param>
        public ServiceNowProactiveState(IStorage storage)
            : base(storage, StorageKey)
        {
        }

        /// <inheritdoc />
        /// <summary>Gets the storage key for caching state information.</summary>
        /// <param name="turnContext">A <see cref="T:Microsoft.Bot.Builder.ITurnContext" /> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>The storage key.</returns>
        protected override string GetStorageKey(ITurnContext turnContext) => StorageKey;
    }
}
