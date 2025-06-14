﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkDownloader
{
    internal static class GlobalSettings
    {
        static GlobalSettings()
        {
            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);
        }

        internal static string RootPath = AppContext.BaseDirectory;
        internal static string OutputPath = Path.Combine(RootPath, "output");
        internal static readonly int MaxDegreeOfParallelism = 5;
    }
}
