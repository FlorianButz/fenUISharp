using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using FenUISharp.Logging;

namespace FenUISharp
{
    public static class ContentExtractor
    {
        public static string ExtractToFile(
            string shortOrFullResourceName,
            string destinationPath,
            Assembly assembly = null,
            bool overwrite = false,
            string expectedSha256 = null)
        {
            assembly ??= Assembly.GetCallingAssembly();

            assembly.GetManifestResourceNames().ToList().ForEach(x => FLogger.Log(x));

            var resourceName = ResolveResourceName(assembly, shortOrFullResourceName);
            if (resourceName == null)
                throw new ArgumentException($"Resource \"{shortOrFullResourceName}\" not found in {assembly.FullName}.");

            if (File.Exists(destinationPath) && !overwrite)
            {
                if (!string.IsNullOrWhiteSpace(expectedSha256))
                    EnsureHash(destinationPath, expectedSha256);
                return destinationPath;
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName)
                   ?? throw new InvalidOperationException($"Failed to open resource stream: {resourceName}"))
            using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.CopyTo(fs);
            }

            if (!string.IsNullOrWhiteSpace(expectedSha256))
                EnsureHash(destinationPath, expectedSha256);

            return destinationPath;
        }

        public static string ExtractToTemp(
            string shortOrFullResourceName,
            string fileName = null,
            Assembly assembly = null,
            bool overwrite = false,
            string expectedSha256 = null)
        {
            fileName ??= shortOrFullResourceName.Split('.').Last(); // fallback
            var path = Path.Combine(Path.GetTempPath(), fileName);
            return ExtractToFile(shortOrFullResourceName, path, assembly, overwrite, expectedSha256);
        }

        public static byte[] ExtractToMemory(string shortOrFullResourceName, Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            var resourceName = ResolveResourceName(assembly, shortOrFullResourceName)
                               ?? throw new ArgumentException($"Resource \"{shortOrFullResourceName}\" not found.");

            using var stream = assembly.GetManifestResourceStream(resourceName)
                              ?? throw new InvalidOperationException($"Failed to open resource stream: {resourceName}");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public static string[] ListAll(Assembly assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            return assembly.GetManifestResourceNames();
        }

        private static string ResolveResourceName(Assembly assembly, string shortOrFull)
        {
            // exact match first
            var all = assembly.GetManifestResourceNames();
            var exact = all.FirstOrDefault(n => n.Equals(shortOrFull, StringComparison.Ordinal));
            if (exact != null) return exact;

            // endswith match
            var matches = all.Where(n => n.EndsWith(shortOrFull, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count switch
            {
                0 => null,
                1 => matches[0],
                _ => throw new ArgumentException(
                    $"Resource name \"{shortOrFull}\" is ambiguous. Candidates:\n{string.Join("\n", matches)}")
            } ?? "";
        }

        private static void EnsureHash(string path, string expectedSha256)
        {
            var actual = ComputeSha256(path);
            if (!actual.Equals(NormalizeHash(expectedSha256), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"SHA-256 mismatch for {path}. Expected {expectedSha256}, got {actual}.");
        }

        private static string ComputeSha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            var hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string NormalizeHash(string h) =>
            h.Replace(" ", "").Replace("-", "").ToLowerInvariant();
    }
}
