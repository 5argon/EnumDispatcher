using System;
using Unity.Entities;

namespace E7.EnumDispatcher
{
    /// <summary>
    /// A system holding `event`. Each <see cref="Dispatch(DispatchAction)"> goes to all receivers of this `event`.
    /// System like <see cref="ActionHandlerSystem"> is automatically a dispatch subscriber on its <see cref="ScriptBehaviourManager.OnCreateManager">.
    /// </summary>
    public class DispatchingSystem : ComponentSystem
    {
        private event ActionHandlerDelegate DispatchTargets;
        private EnumTypeManager ETM;

        /// <summary>
        /// Create an action and dispatch.
        /// </summary>
        public void Dispatch<T>(T e, 
            params (Enum, object)[] payload)
        where T : struct, IConvertible
        => Dispatch(DispatchAction.Create<T>(ETM, e, payload));

        /// <summary>
        /// Signal that something changed. It is useful for stores to react to non-ECS data.
        /// </summary>
        public void SignalChanged<T>(T e, 
            params (Enum, object)[] payload)
        where T : struct, IConvertible
        => Dispatch(ChangedSignal.Create<T>(ETM, e, payload));

        /// <summary>
        /// Dispatch with pre-created action. Use <see cref="DispatchAction.Create{ENUM}(ENUM, (Enum key, object pl)[])"> method to create and cache an action.
        /// </summary>
        public void Dispatch(DispatchAction da) => DispatchTargets?.Invoke(da);

        /// <summary>
        /// You could subscribe with any out-of-ECS callback, but remember to <see cref="Unsubscribe(ActionHandlerDelegate)"> as well.
        /// </summary>
        public void Subscribe(ActionHandlerDelegate handler) => DispatchTargets += handler;
        public void Unsubscribe(ActionHandlerDelegate handler) => DispatchTargets -= handler;

        protected override void OnCreateManager() 
        {
            base.OnCreateManager();
            ETM = EnumTypeManager.Singleton;
            Enabled = false;
        }

        protected override void OnUpdate() { }
    }
}
