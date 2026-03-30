using System.IO;
using System.Text;

namespace Martian.Save.Runtime
{
    public static class SaveFileWriter
    {
        private static readonly UTF8Encoding Utf8NoBom = new(false);

        public static void WriteWithBackup(string fullPath, string content, string backupExtension)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = $"{fullPath}.tmp";
            var backupPath = GetBackupPath(fullPath, backupExtension);

            File.WriteAllText(tempPath, content, Utf8NoBom);

            try
            {
                if (File.Exists(fullPath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    File.Replace(tempPath, fullPath, backupPath, true);
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public static string GetBackupPath(string fullPath, string backupExtension)
        {
            return string.IsNullOrWhiteSpace(backupExtension)
                ? $"{fullPath}.bak"
                : $"{fullPath}{backupExtension}";
        }
    }
}
