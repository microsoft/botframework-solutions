using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace JsonConverter
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Input skill proj root folder: ");
            var rootFolder = Console.ReadLine();

            ConvertJsonFilesToLG(rootFolder);

            GenerateEntryFiles(rootFolder);

            CopySharedLGFiles(rootFolder);

            ModifyCardParameters(rootFolder);

            Console.WriteLine("Done.");
        }
    }
}
