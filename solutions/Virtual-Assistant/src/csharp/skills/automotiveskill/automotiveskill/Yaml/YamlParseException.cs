// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace AutomotiveSkill.Yaml
{
    /// <summary>
    /// Something went wrong while parsing some YAML data.
    /// </summary>
    public class YamlParseException : Exception
    {
        public YamlParseException()
        {
        }

        public YamlParseException(string message)
            : base(message)
        {
        }

        public YamlParseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
