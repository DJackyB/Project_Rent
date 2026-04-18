using Martian.Save;

namespace Martian.Save.Runtime
{
    public sealed class PreparedSaveSectionState
    {
        public PreparedSaveSectionState(ISaveSection section, object state)
        {
            Section = section;
            State = state;
        }

        public ISaveSection Section { get; }
        public object State { get; }
    }
}
