using Microsoft.Bot.Solutions;

namespace Microsoft.Bot.Solutions.Data
{
    public class ConfigData
    {
        private static ConfigData _instance = new ConfigData();

        private int _maxReadSize = Util.CommonUtil.MaxReadSize;
        private int _maxDisplaySize = Util.CommonUtil.MaxDisplaySize;

        private ConfigData()
        {
            _maxReadSize = Util.CommonUtil.MaxReadSize;
            _maxDisplaySize = Util.CommonUtil.MaxDisplaySize;
        }

        public int MaxReadSize
        {
            get
            {
                return _maxReadSize;
            }

            set
            {
                if (value < Util.CommonUtil.MaxReadSize)
                {
                    _maxReadSize = value;
                }
            }
        }

        public int MaxDisplaySize
        {
            get
            {
                return _maxDisplaySize;
            }

            set
            {
                if (value < Util.CommonUtil.MaxDisplaySize)
                {
                    _maxDisplaySize = value;
                }
            }
        }

        public static ConfigData GetInstance()
        {
            return _instance;
        }
    }
}
