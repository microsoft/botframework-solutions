using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualAssistant.Tests.TestHelpers
{
    /// <summary>
    /// Extended ExpectedException attribute to also verify the exception message is what we expect.
    /// </summary>
    public sealed class ExpectedExceptionAndMessage : ExpectedExceptionBaseAttribute
    {
        private readonly Type expectedExceptionType;
        private readonly string expectedExceptionMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedExceptionAndMessage"/> class.
        /// </summary>
        /// <param name="expectedExceptionType">The expected exception.</param>
        public ExpectedExceptionAndMessage(Type expectedExceptionType)
        {
            this.expectedExceptionType = expectedExceptionType;
            this.expectedExceptionMessage = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedExceptionAndMessage"/> class.
        /// </summary>
        /// <param name="expectedExceptionType">The expected exception.</param>
        /// <param name="expectedExceptionMessage">The expected exception message.</param>
        public ExpectedExceptionAndMessage(Type expectedExceptionType, string expectedExceptionMessage)
        {
            this.expectedExceptionType = expectedExceptionType;
            this.expectedExceptionMessage = expectedExceptionMessage;
        }

        /// <summary>
        /// Verify that the exception matches what is expected.
        /// </summary>
        /// <param name="exception">The exception to verify.</param>
        protected override void Verify(Exception exception)
        {
            Assert.IsNotNull(exception);

            Assert.IsInstanceOfType(exception, this.expectedExceptionType, $"Expected Exception of type {this.expectedExceptionType.Name} but received {exception.GetType().Name} instead");

            if (!this.expectedExceptionMessage.Length.Equals(0))
            {
                Assert.AreEqual(this.expectedExceptionMessage, exception.Message, $"Received the expected exception but message was expected to be {this.expectedExceptionMessage} but received {exception.Message} instead.");
            }
        }
    }
}
