// Copyright (c) Softlanding Solutions Inc. All rights reserved.
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace RBAC_Automation
{
    class TextHelper
    {
        public static void WriteTextFile(string path, string text)
        {
            // Create a file to write to.
            File.WriteAllText(path, text);
            string readText = File.ReadAllText(path);
        }

        public static void WriteTextFileAppend(string path, string text)
        {
            // Create a file to write to.
            File.AppendAllText(path, text + Environment.NewLine);
            //string readText = File.ReadAllText(path);
        }
    }
}
