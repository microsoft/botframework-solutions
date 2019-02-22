using PhoneSkill.Common;
using PhoneSkill.Models;

namespace PhoneSkill.Services
{
    public interface IServiceManager
    {
        IContactProvider GetContactProvider(string token, ContactSource source);
    }
}
