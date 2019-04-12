using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    ///  Context to share state between Bots and Skills.
    /// </summary>
    public class SkillContext
    {
        private readonly Dictionary<string, object> _contextStorage = new Dictionary<string, object>();

        public SkillContext()
        {
        }

        public SkillContext(Dictionary<string,object> data)
        {
            _contextStorage = data;
        }

        public int Count
        {
            get { return _contextStorage.Count; }
        }

        public object this[string name]
        {
            get
            {
                return _contextStorage[name];
            }

            set
            {
                _contextStorage[name] = value;
            }
        }

        public bool TryGetValue(string key, out object value)
        {
            return _contextStorage.TryGetValue(key, out value);
        }
    }
}
