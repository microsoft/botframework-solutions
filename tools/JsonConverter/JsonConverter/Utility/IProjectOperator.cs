// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace JsonConverter.Utility
{
    public interface IProjectOperator
    {
        void DeleteFile(string file);

        void AddFileWithCopy(string file);

        void AddFileWithTool(string file);

        void Save();
    }
}
