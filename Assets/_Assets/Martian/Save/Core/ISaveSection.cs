using System;

namespace Martian.Save
{
    public interface ISaveSection
    {
        string Key { get; }
        bool IsRequired { get; }
        Type StateType { get; }
        object CaptureState();
        bool TryValidate(object state, out string error);
        void ApplyState(object state);
    }
}
