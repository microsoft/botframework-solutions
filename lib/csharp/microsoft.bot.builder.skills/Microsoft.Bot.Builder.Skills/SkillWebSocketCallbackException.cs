using System;

namespace Microsoft.Bot.Builder.Skills
{
	public class SkillWebSocketCallbackException : Exception
	{
		public SkillWebSocketCallbackException(string message, Exception ex)
			: base(message, ex)
		{
		}
	}
}