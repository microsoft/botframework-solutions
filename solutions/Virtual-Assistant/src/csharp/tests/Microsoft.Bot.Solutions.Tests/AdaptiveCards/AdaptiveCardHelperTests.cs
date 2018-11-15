using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Bot.Solutions.AdaptiveCards;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.AdaptiveCards
{
    [TestClass]
    public class AdaptiveCardHelperTests
    {
        private const string cardJson = "AdaptiveCards/TestCard.json";

        [TestMethod]
        public void Test_GetCardFromJson()
        {
            List<string> testData = new List<string>();
            // 1# case with \r\n
            testData.Add("Hello \r\n\r\nYour agenda for Tuesday, November 13, 2018\r\n\r\n\r\nMulti-day\r\nEnds Wed 11/21\r\n\r\n\r\n\r\n\r\n\r\n\r\nvacation\r\n\r\n\r\n\r\n\r\n\r\n\r\nAgenda mail settings\r\nUnsubscribe • Privacy statement\r\nMicrosoft Corporation, One Microsoft Way, Redmond, WA 98052");
            // 2# case with "
            testData.Add("\"");
            // 3# case with '
            testData.Add("\'");
            // 4# case with \
            testData.Add("2\\3");
            // 5# case with \\
            testData.Add("\\\\");
            // 6# case with /
            testData.Add("/");
            // 7# case with \b
            testData.Add("\b");
            // 8# case with \f
            testData.Add("\f");
            // 9# case with \n
            testData.Add("\n");
            // 10# case with \r
            testData.Add("\r");
            // 11# case with \t
            testData.Add("\t");

            foreach (var testContent in testData)
            {
                var card = AdaptiveCardHelper.GetCardFromJson(cardJson, GetSpecialCharToken(testContent));

                Assert.IsNotNull(card);
            }
        }

        private StringDictionary GetSpecialCharToken(string content)
        {
            var result = new StringDictionary();

            result.Add("content", content);
            result.Add("receiveddatetime", "Today at 3:35 AM");
            result.Add("subject", content);
            result.Add("speak", content);
            result.Add("link", "https://outlook.live.com/");
            result.Add("searchtype", "relevant unread");
            result.Add("text", "Test");
            result.Add("sender", "Microsoft Outlook Calendar");
            result.Add("namelist", "To: Test");

            return result;
        }
    }
}
