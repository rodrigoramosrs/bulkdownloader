using BulkDownloader.Service;
using BulkDownloader.Utils;
using System.Runtime.InteropServices.Marshalling;

namespace BulkDownloader
{
    internal class Program
    {


        private static async Task Main(string[] args)
        {
            PrintHeader();
            Console.WriteLine("");

            if (args.Length <= 0)
            {
                Console.WriteLine("[!] No argument found to start bulk downloader.");
                Console.WriteLine("[!] place any file path as argument to use this app.");
                Console.WriteLine("[!] example: bulkdownloader \"file1.txt\" \"file2.txt\"");
                Environment.Exit(-1);
            }


            Console.WriteLine($"[ INFO ]");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine($"Root Path: {GlobalSettings.RootPath}");
            Console.WriteLine($"Output Path: {GlobalSettings.OutputPath}");
            Console.WriteLine($"");
            Console.WriteLine($"");


            int argNumber = 0;
            foreach (var file in args)
            {
                argNumber++;
                if (!File.Exists(file))
                {
                    Console.WriteLine($"[!] Argument '{argNumber}' is not a valid path:" + file);
                    continue;
                }

                Console.WriteLine("[i] Downloading content from file: " + file);
                await Service.BulkDownloadService.BulkDownload(file);
            }
            Console.WriteLine("Done...");

        }

        private static void PrintHeader()
        {
            Console.WriteLine(@"- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - ");
            Console.WriteLine(@"  _____       _ _    _____                    _                 _            ");
            Console.WriteLine(@" | ___ \     | | |  |  _  \                  | |               | |           ");
            Console.WriteLine(@" | |_/ /_   _| | | _| | | |_____      ___ __ | | ___   __ _  __| | ___ _ __  ");
            Console.WriteLine(@" | ___ \ | | | | |/ / | | / _ \ \ /\ / / '_ \| |/ _ \ / _` |/ _` |/ _ \ '__| ");
            Console.WriteLine(@" | |_/ / |_| | |   <| |/ / (_) \ V  V /| | | | | (_) | (_| | (_| |  __/ |    ");
            Console.WriteLine(@" \____/ \__,_|_|_|\_\___/ \___/ \_/\_/ |_| |_|_|\___/ \__,_|\__,_|\___|_|    ");
            Console.WriteLine(@"- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - ");
            Console.WriteLine("by: Rodrigo Ramos ( https://github.com/rodrigoramosrs ) ");
            Console.WriteLine("");
        }



    }
}
