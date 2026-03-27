using System;
using System.Collections.Generic;
using System.IO;
using Martian.Save;
using UnityEngine;

namespace Martian.Save.Runtime
{
    public sealed class PersistentDataPathSaveStorage : ISaveStorage
    {
        private readonly string _rootPath;

        public PersistentDataPathSaveStorage(string rootPath = null)
        {
            _rootPath = string.IsNullOrWhiteSpace(rootPath)
                ? Application.persistentDataPath
                : rootPath;
        }

        public bool Exists(string relativePath)
        {
            return File.Exists(ToFullPath(relativePath));
        }

        public string ReadAllText(string relativePath)
        {
            return File.ReadAllText(ToFullPath(relativePath));
        }

        public void WriteAllText(string relativePath, string content, string backupExtension)
        {
            SaveFileWriter.WriteWithBackup(ToFullPath(relativePath), content, backupExtension);
        }

        public void Delete(string relativePath, string backupExtension = null)
        {
            var fullPath = ToFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            var backupPath = SaveFileWriter.GetBackupPath(fullPath, backupExtension);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        public IReadOnlyList<string> ListFiles(string relativeDirectory, string fileExtension)
        {
            var fullDirectory = ToFullPath(relativeDirectory);
            if (!Directory.Exists(fullDirectory))
            {
                return Array.Empty<string>();
            }

            var extension = string.IsNullOrWhiteSpace(fileExtension) ? "*.*" : $"*{fileExtension}";
            var files = Directory.GetFiles(fullDirectory, extension, SearchOption.TopDirectoryOnly);
            var results = new List<string>(files.Length);

            for (int i = 0; i < files.Length; i++)
            {
                var relativePath = Path.GetRelativePath(_rootPath, files[i]);
                results.Add(relativePath.Replace('\\', '/'));
            }

            return results;
        }

        private string ToFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Relative path must not be empty.", nameof(relativePath));
            }

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_rootPath, normalized);
        }
    }
}
