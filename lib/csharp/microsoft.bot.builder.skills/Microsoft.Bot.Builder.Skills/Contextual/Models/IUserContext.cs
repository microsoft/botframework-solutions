using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Contextual.Models
{
    public interface IUserContext
    {
        Person GetManager(string contactName);

        string GetMyPhoneNumber();

        string GetMyAddress();

        Person GetPronounUser(string pron);

        string GetPronounItem(string pron);

        Person GetUser();

        void SavePronounUser(string pron);

        void SavePronounItem(string pron);
    }
}
