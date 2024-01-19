using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkDownloader.Utils
{
    internal static class FileUtils
    {
        internal static bool CreateFolderIfNotExists(string Filename)
        {
            if (string.IsNullOrEmpty(Filename))
            {
                return false;
            }
            if (!Directory.Exists(Path.GetDirectoryName(Filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Filename));
            }
            return true;
        }

        internal static string ConvertUriToLocalPath(Uri uri, string RootPath)
        {
            return $"{RootPath}/{uri.Host}/{uri.LocalPath}";
        }
    }
}
