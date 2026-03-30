using System;

namespace Martian.Save
{
    [Serializable]
    public sealed class SaveSlotSummary
    {
        public string slotId;
        public string displayName;
        public string savedAtUtc;
        public int schemaVersion;
        public string appVersion;
    }
}
