using System.Collections.Generic;

namespace Martian.Save
{
    public interface ISaveStorage
    {
        bool Exists(string relativePath);
        string ReadAllText(string relativePath);
        void WriteAllText(string relativePath, string content, string backupExtension);
        void Delete(string relativePath, string backupExtension = null);
        IReadOnlyList<string> ListFiles(string relativeDirectory, string fileExtension);
    }
}
