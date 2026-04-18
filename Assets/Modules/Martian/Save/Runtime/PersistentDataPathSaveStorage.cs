using System;
using System.Collections.Generic;
using System.IO;
using Martian.Save;
using UnityEngine;

namespace Martian.Save.Runtime
{
    /// <summary>
    /// 文件系统存储实现。使用本地文件存储存档数据。
    ///
    /// 存储位置：
    /// - 默认：Application.persistentDataPath（平台相关的持久化目录）
    ///   - PC: C:\Users\[User]\AppData\LocalLow\[Company]\[Product]
    ///   - Android: /data/data/[package]/files
    ///   - iOS: Application Support 目录
    /// - 或传入自定义根路径
    ///
    /// 特性：
    /// - 支持备份机制：WriteAllText 将原文件备份后再写入新文件
    /// - 原子性：使用临时文件 + 重命名确保写入的原子性
    /// - 路径规范化：自动处理斜杠转换，支持跨平台
    /// </summary>
    public sealed class PersistentDataPathSaveStorage : ISaveStorage
    {
        /// <summary>文件存储的根目录绝对路径。</summary>
        private readonly string _rootPath;

        /// <summary>
        /// 构造存储实例。
        /// </summary>
        /// <param name="rootPath">根目录路径，或 null 使用 Application.persistentDataPath</param>
        public PersistentDataPathSaveStorage(string rootPath = null)
        {
            _rootPath = string.IsNullOrWhiteSpace(rootPath)
                ? Application.persistentDataPath
                : rootPath;
        }

        /// <summary>
        /// 检查文件是否存在。
        /// </summary>
        public bool Exists(string relativePath)
        {
            return File.Exists(ToFullPath(relativePath));
        }

        /// <summary>
        /// 读取文件全部内容。
        /// </summary>
        public string ReadAllText(string relativePath)
        {
            return File.ReadAllText(ToFullPath(relativePath));
        }

        /// <summary>
        /// 写入文件内容。调用 SaveFileWriter 确保原子性和备份。
        /// </summary>
        public void WriteAllText(string relativePath, string content, string backupExtension)
        {
            SaveFileWriter.WriteWithBackup(ToFullPath(relativePath), content, backupExtension);
        }

        /// <summary>
        /// 删除文件和对应的备份文件。
        /// </summary>
        public void Delete(string relativePath, string backupExtension = null)
        {
            var fullPath = ToFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // 也删除备份文件
            var backupPath = SaveFileWriter.GetBackupPath(fullPath, backupExtension);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        /// <summary>
        /// 列出目录下的所有匹配扩展名的文件。
        /// 返回相对路径列表（使用正斜杠）。
        /// </summary>
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
                // 规范化路径：统一使用正斜杠
                results.Add(relativePath.Replace('\\', '/'));
            }

            return results;
        }

        /// <summary>
        /// 将相对路径转换为完整绝对路径。
        /// 自动处理正斜杠 -> 平台路径分隔符的转换。
        /// </summary>
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
