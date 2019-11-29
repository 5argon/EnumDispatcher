using System;
using Unity.Entities;
using UnityEngine;

namespace E7.EnumDispatcher
{
    public delegate void ActionHandlerDelegate(DispatchAction action);

    /// <summary>
    /// Dispatcher static class access a <see cref="DispatchingSystem"> in your active world.
    /// System like <see cref="ActionHandlerSystem"> is automatically a dispatch subscriber on its <see cref="ScriptBehaviourManager.OnCreate">.
    /// You can also make a <see cref="StateStore{STATE}"> handle action with <see cref="StateStore{STATE}.EnableStoreAction">.
    /// </summary>
    public static class Dispatcher
    {
        /// <summary>
        /// Returns `null` when no active world.
        /// </summary>
        /// <value></value>
        public static DispatchingSystem Active => World.DefaultGameObjectInjectionWorld == null ? null : Of(World.DefaultGameObjectInjectionWorld);

        public static DispatchingSystem Of(World w) => w.GetOrCreateSystem<DispatchingSystem>();

        /// <summary>
        /// Dispatch to a dispatcher of the currently active world.
        /// </summary>
        public static void Dispatch<T>(T e,
            params (Enum, object)[] payload)
        where T : struct, IConvertible
        {
            DispatchingSystem activeDs = Active;
            if (activeDs == null)
            {
                throw new Exception($"You cannot dispatch an action to an empty active world.");
            }
            activeDs.Dispatch(e, payload);
        }

        /// <summary>
        /// Signal that something changed to the currently active world.
        /// </summary>
        public static void SignalChanged<T>(T e)
        where T : struct, IConvertible
        {
            DispatchingSystem activeDs = Active;
            if (activeDs == null)
            {
                throw new Exception($"You cannot dispatch an action to an empty active world.");
            }
            activeDs.SignalChanged(e);
        }

        /// <summary>
        /// Dispatch to a dispatcher of the currently active world with pre-created action.
        /// Use `DispatchAction.Create` method to create and cache an action.
        /// </summary>
        public static void Dispatch(DispatchAction da)
        {
            DispatchingSystem activeDs = Active;
            if (activeDs == null)
            {
                throw new Exception($"You cannot dispatch an action to an empty active world.");
            }
            activeDs.Dispatch(da);
        }

        /// <summary>
        /// You could subscribe with any out-of-ECS callback, but remember to <see cref="Unsubscribe(ActionHandlerDelegate)"> as well.
        /// </summary>
        public static void Subscribe(ActionHandlerDelegate handler) 
        {
            Active.Subscribe(handler);
        }

        /// <summary>
        /// Does nothing if <see cref="World.Active"> is `null`. It is safe to use this in `OnDestroy` when the game quits.
        /// </summary>
        public static void Unsubscribe(ActionHandlerDelegate handler) => Active?.Unsubscribe(handler);
    }
}
