using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Common
{
    public class ConfigData
    {
        private static ConfigData _instance = new ConfigData();

        private int _maxDisplaySize = CalendarCommonUtil.MaxDisplaySize;

        private ConfigData()
        {
            _maxDisplaySize = CalendarCommonUtil.MaxDisplaySize;
        }

        public int MaxDisplaySize
        {
            get
            {
                return _maxDisplaySize;
            }

            set
            {
                if (value < CalendarCommonUtil.MaxDisplaySize)
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
