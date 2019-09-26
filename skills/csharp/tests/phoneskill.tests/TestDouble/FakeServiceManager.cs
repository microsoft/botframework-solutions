using PhoneSkill.Common;
using PhoneSkill.Models;
using PhoneSkill.Services;

namespace PhoneSkill.Tests.TestDouble
{
    public class FakeServiceManager : IServiceManager
    {
        private IContactProvider contactProvider;

        public FakeServiceManager()
        {
            contactProvider = new StubContactProvider();
        }

        public IContactProvider GetContactProvider(string token, ContactSource source)
        {
            return contactProvider;
        }
    }
}
