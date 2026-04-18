using System;
using System.Collections.Generic;

namespace Martian.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int schemaVersion;
        public string savedAtUtc;
        public string slotId;
        public string displayName;
        public string appVersion;
        public List<SaveSectionRecord> sections = new();
    }
}
