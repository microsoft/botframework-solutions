// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkill.Utilities
{
    public class CalendarConfigData
    {
        private static CalendarConfigData _instance = new CalendarConfigData();

        private int _maxDisplaySize = CalendarCommonUtil.MaxDisplaySize;

        private CalendarConfigData()
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

        public static CalendarConfigData GetInstance()
        {
            return _instance;
        }
    }
}
