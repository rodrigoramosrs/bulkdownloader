using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkDownloader.Utils
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    internal static class FileUtils
    {
        /* ------------------------------------------------------------------
           1.  GARANTE QUE A PASTA (OU A PASTA-PAI DO ARQUIVO) EXISTA
        ------------------------------------------------------------------ */
        internal static bool EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            try
            {
                // Se contiver extensão, consideramos que é um arquivo
                var dir = Path.HasExtension(path) ? Path.GetDirectoryName(path)! : path;

                if (string.IsNullOrWhiteSpace(dir)) return false;

                Directory.CreateDirectory(dir);        // cria recursivamente
                return Directory.Exists(dir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao criar diretório: {ex.Message}");
                return false;
            }
        }

        /* ------------------------------------------------------------------
           2.  CONVERTE Uri → CAMINHO LOCAL   (mantém separadores originais)
        ------------------------------------------------------------------ */
        internal static string ConvertUriToLocalPath(Uri uri, string rootPath)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Root path não pode ser vazio", nameof(rootPath));

            // Normaliza o rootPath e remove / ou \ finais
            rootPath = Path.GetFullPath(rootPath)
                           .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Host é sempre o primeiro segmento
            var sanitizedHost = SanitizeSegment(uri.Host);

            // Divide o LocalPath, ignora vazios e “.” / “..”
            var sanitizedSegments =
                uri.LocalPath
                   .Split('/', StringSplitOptions.RemoveEmptyEntries)
                   .Where(p => p != "." && p != "..")
                   .Select(SanitizeSegment);

            // Combina tudo de modo multiplataforma
            string fullPath = Path.Combine(new[] { rootPath, sanitizedHost }.Concat(sanitizedSegments).ToArray());

            return fullPath;
        }

        /* ------------------------------------------------------------------
           3.  CONVERTE string-URL → CAMINHO LOCAL                       (stub)
        ------------------------------------------------------------------ */
        internal static string ConvertUrlToLocalPath(string url, string rootPath)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL não pode ser vazia", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new UriFormatException("URL inválida");

            return ConvertUriToLocalPath(uri, rootPath);
        }

        /* ------------------------------------------------------------------
           4.  UTILITÁRIO: sanitiza UM segmento (arquivo ou diretório)
               – mantém / \ intactos; substitui apenas caracteres ilegais
        ------------------------------------------------------------------ */
        private static string SanitizeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) return string.Empty;

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(segment.Length);

            foreach (char c in segment)
                sb.Append(invalid.Contains(c) ? '_' : c);

            return sb.ToString();
        }
    }
}
