using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Extensions
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class OAuthProviderExtensionsTest
    {
        [TestMethod]
        public void Test_GoogleOAuthExtension()
        {
            var provider = OAuthProviderExtensions.GetAuthenticationProvider("Google");

            Assert.AreEqual(OAuthProvider.Google, provider);
        }

        [TestMethod]
        public void Test_TodoistOAuthExtension()
        {
            var provider = OAuthProviderExtensions.GetAuthenticationProvider("Todoist");

            Assert.AreEqual(OAuthProvider.Todoist, provider);
        }

        [TestMethod]
        public void Test_GenericOauth2OAuthExtension()
        {
            var provider = OAuthProviderExtensions.GetAuthenticationProvider("Oauth 2 Generic Provider");

            Assert.AreEqual(OAuthProvider.GenericOauth2, provider);
        }

        [TestMethod]
        public void Test_TestOauth2OAuthExtension()
        {
            var ex = Assert.ThrowsException<Exception>(() => OAuthProviderExtensions.GetAuthenticationProvider("Test"));
            Assert.IsTrue(ex.Message.Contains("could not be parsed"));
        }
    }
}
