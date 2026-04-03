using System.Collections.Generic;

namespace Martian.Save
{
    /// <summary>
    /// 存档存储接口。负责存档文件的读写和管理。
    ///
    /// 三层架构中的第三层（存储层）：
    /// - SaveService 调用此接口进行文件 I/O
    /// - 实现可使用本地文件系统、云端、内存等存储后端
    /// - 负责备份、路径规范化等细节
    ///
    /// 路径约定：
    /// - relativePath：相对于根路径的路径（如 "Saves/slot1.json"）
    /// - 实现负责将相对路径转换为完整路径
    /// - 所有路径使用正斜杠 "/" 分隔（跨平台）
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>
        /// 检查文件是否存在。
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>文件存在返回 true</returns>
        bool Exists(string relativePath);

        /// <summary>
        /// 读取文件全部文本内容。
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <returns>文件内容</returns>
        /// <exception cref="System.IO.FileNotFoundException">文件不存在时抛出</exception>
        string ReadAllText(string relativePath);

        /// <summary>
        /// 写入文件文本内容（原子性：写成功后才替换原文件）。
        /// 实现应：
        /// 1. 写入临时文件
        /// 2. 若原文件存在，创建备份（使用 backupExtension）
        /// 3. 原子地替换原文件为临时文件
        /// 这样可防止写入过程中的电源中断导致存档损坏。
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <param name="content">文件内容</param>
        /// <param name="backupExtension">备份文件后缀（如 ".bak"）</param>
        void WriteAllText(string relativePath, string content, string backupExtension);

        /// <summary>
        /// 删除文件。
        /// 如果提供了 backupExtension，也删除对应的备份文件。
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <param name="backupExtension">备份文件后缀（可选），如果提供则一并删除</param>
        void Delete(string relativePath, string backupExtension = null);

        /// <summary>
        /// 列出目录下所有匹配扩展名的文件。
        /// 返回相对路径列表（使用正斜杠分隔）。
        /// </summary>
        /// <param name="relativeDirectory">相对目录路径</param>
        /// <param name="fileExtension">文件扩展名（如 ".json"），或 null 返回所有文件</param>
        /// <returns>相对路径列表（不递归子目录）</returns>
        IReadOnlyList<string> ListFiles(string relativeDirectory, string fileExtension);
    }
}
