// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Util
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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
