// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Common
{
    using System.Collections.Generic;
    using System.Linq;

    public class Util
    {
        private Util()
        {
        }

        public static bool NullSafeEquals(string lhs, string rhs)
        {
            if (lhs == null)
            {
                return rhs == null;
            }

            return lhs.Equals(rhs);
        }

        public static IList<T> CopyList<T>(IList<T> original)
        {
            IList<T> copy = new List<T>();
            foreach (var element in original)
            {
                copy.Add(element);
            }

            return copy;
        }

        public static bool IsNullOrEmpty<T>(IList<T> list)
        {
            return list == null || !list.Any();
        }

        public static bool IsChangeIntent(string intent)
        {
            return "VEHICLE_SETTINGS_CHANGE".Equals(intent) || "VEHICLE_SETTINGS_DECLARATIVE".Equals(intent);
        }

        public static bool IsCheckIntent(string intent)
        {
            return "VEHICLE_SETTINGS_CHECK".Equals(intent);
        }
    }
}