// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Protocol.StreamingExtensions.NetCore;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using $safeprojectname$.Responses.Main;
using $safeprojectname$.Services;

namespace $safeprojectname$.Adapters
{
	public class DefaultWebSocketAdapter : WebSocketEnabledHttpAdapter, IUserTokenProvider
	{
		private readonly MicrosoftAppCredentials _appCredentials;

		public DefaultWebSocketAdapter(
			IConfiguration config,
			BotSettings settings,
			ICredentialProvider credentialProvider,
			IBotTelemetryClient telemetryClient,
			BotStateSet botStateSet,
			MicrosoftAppCredentials appCredentials)
			: base(config, credentialProvider)
		{
			_appCredentials = appCredentials ?? throw new ArgumentNullException("AppCredentials were not passed which are required for speech enabled authentication scenarios.");

			OnTurnError = async (turnContext, exception) =>
			{
				await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.Message}"));
				await turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"{exception.StackTrace}"));
				await turnContext.SendActivityAsync(MainStrings.ERROR);
				telemetryClient.TrackException(exception);
			};

			Use(new TranscriptLoggerMiddleware(new AzureBlobTranscriptStore(settings.BlobStorage.ConnectionString, settings.BlobStorage.Container)));
			Use(new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true));
			Use(new ShowTypingMiddleware());
			Use(new SetLocaleMiddleware(settings.DefaultLocale ?? "en-us"));
			Use(new EventDebuggerMiddleware());
			Use(new AutoSaveStateMiddleware(botStateSet));
		}

		public Task<Dictionary<string, TokenResponse>> GetAadTokensAsync(ITurnContext context, string connectionName, string[] resourceUrls, string userId = null, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public Task<string> GetOauthSignInLinkAsync(ITurnContext turnContext, string connectionName, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<string> GetOauthSignInLinkAsync(ITurnContext turnContext, string connectionName, string userId, string finalRedirect = null, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public async Task<TokenStatus[]> GetTokenStatusAsync(ITurnContext context, string userId, string includeFilter = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!string.IsNullOrEmpty(context.Activity.ChannelId) && context.Activity.ChannelId.ToLower() == "directlinespeech")
			{
				BotAssert.ContextNotNull(context);
				if (string.IsNullOrWhiteSpace(userId))
				{
					throw new ArgumentNullException(nameof(userId));
				}

				var client = new OAuthClient(new Uri(OAuthClientConfig.OAuthEndpoint), _appCredentials);
				var result = await client.UserToken.GetTokenStatusAsync(userId, context.Activity?.ChannelId, includeFilter, cancellationToken).ConfigureAwait(false);
				return result?.ToArray();
			}
			else
			{
				throw new Exception("The channel is not directlinespeech so we do not allow token operations!");
			}
		}

		public async Task<TokenResponse> GetUserTokenAsync(ITurnContext turnContext, string connectionName, string magicCode, CancellationToken cancellationToken)
		{
			if (!string.IsNullOrEmpty(turnContext.Activity.ChannelId) && turnContext.Activity.ChannelId.ToLower() == "directlinespeech")
			{
				var client = new OAuthClient(new Uri(OAuthClientConfig.OAuthEndpoint), _appCredentials);

				// Attempt to retrieve the token directly, we can't prompt the user for which Token to use so go with the first
				// Moving forward we expect to have a "default" choice as part of Linked Accounts,.
				var tokenResponse = await client.UserToken.GetTokenWithHttpMessagesAsync(
				turnContext.Activity.From.Id,
				connectionName,
				turnContext.Activity.ChannelId,
				null,
				null,
				cancellationToken).ConfigureAwait(false);

				return tokenResponse?.Body;
			}
			else
			{
				throw new Exception("The channel is not directlinespeech so we do not allow token operations!");
			}
		}

		public Task SignOutUserAsync(ITurnContext turnContext, string connectionName = null, string userId = null, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}