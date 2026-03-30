using System;

namespace Martian.Save
{
    [Serializable]
    public sealed class SaveSectionRecord
    {
        public string key;
        public string jsonPayload;
    }
}
