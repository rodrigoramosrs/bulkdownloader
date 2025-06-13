using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BulkDownloader.Service
{
    internal static class HttpService
    {
        internal static readonly HttpClientHandler _httpClienthandler;
        internal static readonly HttpClient _httpClient;
        static HttpService()
        {
            _httpClienthandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClienthandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            _httpClienthandler.UseDefaultCredentials = true;
            _httpClienthandler.AllowAutoRedirect = true;
            _httpClienthandler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            _httpClienthandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            _httpClienthandler.CookieContainer = new CookieContainer();
            _httpClienthandler.MaxAutomaticRedirections = 50;
            _httpClienthandler.MaxConnectionsPerServer = 20;
            _httpClienthandler.MaxRequestContentBufferSize = 10485760;
            _httpClienthandler.PreAuthenticate = true;
            _httpClienthandler.Proxy = null;
            _httpClienthandler.UseCookies = true;
            _httpClienthandler.UseProxy = true;

            _httpClient = new HttpClient(_httpClienthandler);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
            _httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            _httpClient.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
        }


        internal static async Task<Stream> GetAsStream(string url, CancellationToken cancellationToken)
        {
            var response = await InternalGetResponse(url, cancellationToken);
            return response.Content.ReadAsStream();
        }

        internal static async Task<(bool Success, long FileSize)> GetAndSaveAsFile(string url, string FilenameWithPath, CancellationToken cancellationToken)
        {
            string finalFilePath = FilenameWithPath;
            if (!Path.HasExtension(finalFilePath))
                finalFilePath += ".no_ext";
            try
            {
                using (var stream = await GetAsStream(url, cancellationToken))
                {
                    

                   

                    const int buffer = 64 * 1024; // 64 KB
                    await using var fs = new FileStream(
                        finalFilePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: buffer,
                        options: FileOptions.Asynchronous | FileOptions.SequentialScan);

                    await stream.CopyToAsync(fs, buffer, cancellationToken).ConfigureAwait(false);
                    return (true, fs.Length);

                    //using (var fileStream = new FileStream(FilenameWithPath, FileMode.Create, FileAccess.Write))
                    //{
                    //    await stream.CopyToAsync(fileStream, cancellationToken);
                    //    return (true, fileStream.Length); // Retorna sucesso e tamanho do arquivo

                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar arquivo: {ex.Message}");
                return (false, 0); // Em caso de erro, retorna falso e tamanho zero
            }
        }

        private static async Task<HttpResponseMessage> InternalGetResponse(string url, CancellationToken cancellationToken)
        {
            _httpClient.DefaultRequestHeaders.Referrer = new (url);// new Uri(uri.Host);
            return await _httpClient.GetAsync(url, cancellationToken);
        }
    }
}
