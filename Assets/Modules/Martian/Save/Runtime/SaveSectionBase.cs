using System;
using Martian.Save;

namespace Martian.Save.Runtime
{
    public abstract class SaveSectionBase<TState> : ISaveSection where TState : class
    {
        public abstract string Key { get; }
        public virtual bool IsRequired => true;
        public Type StateType => typeof(TState);

        public object CaptureState()
        {
            return CaptureTypedState();
        }

        public bool TryValidate(object state, out string error)
        {
            if (state is not TState typedState)
            {
                error = $"Section '{Key}' expected state type '{typeof(TState).Name}'.";
                return false;
            }

            return TryValidateTypedState(typedState, out error);
        }

        public void ApplyState(object state)
        {
            if (state is not TState typedState)
            {
                throw new InvalidOperationException($"Section '{Key}' expected state type '{typeof(TState).Name}'.");
            }

            ApplyTypedState(typedState);
        }

        protected abstract TState CaptureTypedState();

        protected virtual bool TryValidateTypedState(TState state, out string error)
        {
            error = null;
            return true;
        }

        protected abstract void ApplyTypedState(TState state);
    }
}
