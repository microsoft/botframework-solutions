// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Bot.Builder.Solutions.Util
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class MD5Util
    {
        private static MD5 md5 = MD5.Create();

        public static string ComputeHash(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                stringBuilder.Append(hashBytes[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return stringBuilder.ToString();
        }
    }
}