using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Resources
{
    public class ResponseManager
    {
        public ResponseManager(IResponseTemplateCollection responseTemplate)
        {
            LoadResponses();
        }

        public ResponseManager(IResponseTemplateCollection[] responseTemplates)
        {
            foreach (var template in responseTemplates)
            {
                LoadResponses();
            }
        }

        public void LoadResponses()
        {

        }

        public void GetResponseTemplate(string templateId, string locale)
        {

        }

        public IActivity GetActivity(string templateId, string locale)
        {
            return null;
        }
    }
}
