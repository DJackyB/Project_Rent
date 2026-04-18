using System;

namespace Martian.Save
{
    [Serializable]
    public sealed class SaveRuntimeOptions
    {
        public int currentSchemaVersion = 1;
        public bool prettyPrintJson = true;
        public string saveFileExtension = ".json";
        public string backupFileExtension = ".bak";
    }
}
