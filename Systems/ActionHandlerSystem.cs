using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace E7.EnumDispatcher
{
    /// <summary>
    /// A system which can receive <see cref="DispatchAction"> and turn them into job scheduling on its <see cref="OnUpdate">.
    /// </summary>
    [UpdateInGroup(typeof(ActionHandlerSystem.ActionHandlerGroup))]
    public abstract class ActionHandlerSystem : JobComponentSystem
    {
        /// <summary>
        /// You could put your system with <see cref="UpdateAfterAttribute"> on this group to ensure it updates after all action-based system.
        /// </summary>
        public static class ActionHandlerGroup { }

        EnumTypeManager ETM;
        DispatchingSystem DispatchingSystem;
        protected Queue<DispatchAction> queuedActions;
        bool created;

        /// <summary>
        /// Please call `base.OnCreateManager()` on your subclass if you have your own override !!
        /// </summary>
        protected override void OnCreateManager()
        {
            ETM = EnumTypeManager.Singleton;
            DispatchingSystem = World.GetOrCreateManager<DispatchingSystem>();
            queuedActions = new Queue<DispatchAction>();
            DispatchingSystem.Subscribe(HandleAction);
            created = true;
        }

#if UNITY_EDITOR
        private void NotCreatedCheck()
        {
            if(!created)
            {
                throw new Exception($"ActionHandlerSystem {this.GetType().Name} was not initialized! Did you forget calling `base.OnCreateManager()`?");
            }
        }
#endif

        /// <summary>
        /// Called immediately synchronously on dispatching action, but it will just queue the action to be
        /// converted to jobs on its turn to update. We have to be in the <see cref="JobComponentSystem">'s "pipeline"
        /// to ensure nice dependency chain.
        /// </summary>
        private void HandleAction(DispatchAction action)
        {
            //Collect jobs to put in dep chain when update arrives.
            queuedActions.Enqueue(new DispatchAction(action));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            while(queuedActions.Count > 0)
            {
                inputDeps = OnAction(queuedActions.Dequeue(), inputDeps);
            }
            return inputDeps;
        }

        /// <summary>
        /// You should use do <see cref="DispatchAction.CastJob">, in order to make <see cref="JobDispatchAction"> which can be copied into a job.
        /// </summary>
        protected abstract JobHandle OnAction(DispatchAction da, JobHandle jobHandle);

        protected override void OnDestroyManager()
        {
            //In the case of world destroying that system might have gone first.
            DispatchingSystem?.Unsubscribe(HandleAction);
        }

        /// <summary>
        /// Call this on your ActionHandlerSystem's OnCreateManager.
        /// Do not dispose ActionCategory, it will be automatically on system's OnDestroyManager.
        /// </summary>
        protected ActionCategory<ENUM> GetActionCategory<ENUM>() where ENUM : struct, IConvertible
        {
#if UNITY_EDITOR
            NotCreatedCheck();
#endif
            return new ActionCategory<ENUM>
            {
                categoryIndex = ETM.GetCategoryIndex<ENUM>(),
                fastConvert = ETM.Category<ENUM>().FastCastDictionary,
            };
        }

        protected ActionFlag GetActionFlag(string flag) 
        {
#if UNITY_EDITOR
            NotCreatedCheck();
#endif
            return new ActionFlag(flag, ETM);
        }

        protected ActionExact GetActionExact<ENUM>(ENUM action) where ENUM : struct, IConvertible
        {
#if UNITY_EDITOR
            NotCreatedCheck();
#endif
            return new ActionExact
            {
                categoryAndTypeIndex = new int2(ETM.GetCategoryIndex<ENUM>(), ETM.Category<ENUM>().FastCastToActionType(action))
            };
        }
    }
}