using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        public static void CopySharedLGFiles(string rootFolder)
        {
            var responseFolder = Path.Combine(rootFolder, "Responses", "Shared");
            Directory.CreateDirectory(responseFolder);

            try
            {
                File.Copy("Shared.lg", Path.Combine(responseFolder, "Shared.lg"), false);
            }
            catch
            { 
            }
        }
    }
}
