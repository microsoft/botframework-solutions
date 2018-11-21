using Microsoft.Bot.Solutions;

namespace Microsoft.Bot.Solutions.Data
{
    public class ConfigData
    {
        private static int _maxReadSize = Util.CommonUtil.MaxReadSize;
        private static int _maxDisplaySize = Util.CommonUtil.MaxDisplaySize;

        public static int MaxReadSize
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

        public static int MaxDisplaySize
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
    }
}
