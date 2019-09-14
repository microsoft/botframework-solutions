// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;

namespace PhoneSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
	{
		public BotController(
			IBot bot,
			BotSettingsBase botSettings,
			IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
			SkillWebSocketAdapter skillWebSocketAdapter)
			: base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter)
		{
		}
	}
}