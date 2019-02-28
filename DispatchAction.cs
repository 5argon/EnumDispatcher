using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace E7.EnumDispatcher
{
    /// <summary>
    /// Special-purpose action.
    /// </summary>
    public enum SignalAction
    {
        /// <summary>
        /// Dispatch this action to ping all stores to update its data based on its own "external data".
        /// Normally system responds to store data change. Sometimes we can't put those data in ECS effectively.
        /// 
        /// The solution is to have the store updates its own data from external data on this signal, then the system could
        /// responds to store data change as usual. The signal acts as a manual "changed" for external data, normally ECS is responsible
        /// to track those changes.
        /// </summary>
        ExternalDataChanged
    }

    /// <summary>
    /// Basically just <see cref="DispatchAction"> in disguise, but serves different purpose.
    /// </summary>
    public class ChangedSignal : DispatchAction { }

    public partial class DispatchAction : Component 
    {
        private EnumTypeManager ETM;

        //An int representing enum's type
        private int enumCategoryIndex;
        //The enum's actual int value
        private int enumActionTypeIndex;
        //Do not dispose! It is statically kept for everyone.
        private NativeArray<int> flags;

        private Dictionary<Enum, object> payload;

        //Just a running number unique to every action
        private int actionId;
        internal int ActionId => actionId;

        public override string ToString() => $"Action Category : {CategoryName} Type : {ActionTypeName}";

        private static int globalActionId;

        /// <summary>
        /// Take the `int` representation of your action and flags with you in the job.
        /// </summary>
        public JobDispatchAction CastJob() => new JobDispatchAction(enumCategoryIndex, enumActionTypeIndex, flags);

        /// <summary>
        /// You can pre-create the action to be dispatched with this.
        /// Each action allocates a `Dictionary` for the payload so it make sense to keep them for reuse.
        /// </summary>
        public static DispatchAction Create<ENUM>(
            ENUM e, 
            params (Enum key, object pl)[] payload 
        )
        where ENUM : struct, IConvertible
        => Create<ENUM>(EnumTypeManager.Singleton, e, payload);

        public static DispatchAction Create<ENUM>(
            EnumTypeManager etm,
            ENUM e, 
            params (Enum key, object pl)[] payload 
        )
        where ENUM : struct, IConvertible
        {
            var payloadDict = new Dictionary<Enum, object>();
            foreach (var x in payload)
            {
                payloadDict.Add(x.key, x.pl);
            }

            globalActionId++;
            return new DispatchAction()
            {
                ETM = etm,
                enumCategoryIndex = etm.GetCategoryIndex<ENUM>(),
                enumActionTypeIndex = etm.Category<ENUM>().FastCastToActionType(e),
                flags = etm.Category<ENUM>().GetFlags(e),
                payload = payloadDict,
                actionId = globalActionId,
            };
        }

        internal DispatchAction() { }

        /// <summary>
        /// Copy values from other DispatchAction without replacing the reference type. Copy even action ID.
        /// </summary>
        internal DispatchAction(DispatchAction da) 
        {
            this.ETM = da.ETM;
            this.enumCategoryIndex = da.enumCategoryIndex;
            this.enumActionTypeIndex = da.enumActionTypeIndex;
            this.payload = da.payload;
            this.actionId = da.actionId;
            this.flags = da.flags;
        }

        public override int GetHashCode() => actionId;

        /// <summary>
        /// `if` on this when handling action, before switch-case on the returned `out` variable.
        /// You can skip the category check if you are handling only one type of enum that represent the action. In that case use `As`.
        /// 
        /// It does not care about integer payload, the cast to enum is cached.
        /// </summary>
        /// <param name="actionType">If the check returns `false`, this value is default(`ENUM`) but you should not use it anyways.</param>
        public bool Category<ENUM>(out ENUM actionType) where ENUM : struct, IConvertible
        {
            bool checkResult = ETM.GetCategoryIndex<ENUM>() == enumCategoryIndex;
            actionType = checkResult ? ETM.Category<ENUM>().FastCastFromActionType(enumActionTypeIndex) : default(ENUM);
            return checkResult;
        }

        /// <summary>
        /// `if` on this when handling action, an overload with discarded `out` you just want to check category.
        /// </summary>
        public bool Category<ENUM>() where ENUM : struct, IConvertible
        => Category<ENUM>(out _);

        /// <summary>
        /// If you want to skip the check on action's category, use `switch case` with value returned from this method.
        /// It might be useful in `StateReactSystem` where you know which category has been handled by the store for some performance.
        /// The cast to enum is cached.
        /// </summary>
        public ENUM As<ENUM>() where ENUM : struct, IConvertible
        => ETM.Category<ENUM>().FastCastFromActionType(enumActionTypeIndex);

        /// <summary>
        /// When you want to `if` on the action directly. This one also check for the correct category.
        /// </summary>
        public bool Is<ENUM>(ENUM actionEnum) where ENUM : struct, IConvertible
        {
            var actionCategory = ETM.GetCategoryIndex<ENUM>();
            var actionType = ETM.Category<ENUM>().FastCastToActionType(actionEnum);
            //Debug.Log($"{this.enumCategoryIndex} == {actionCategory} && {this.enumActionTypeIndex} == {actionType}");
            return this.enumCategoryIndex == actionCategory && this.enumActionTypeIndex == actionType;
        }

        /// <summary>
        /// When you want to `if` on the action directly. It does not care about category. 
        /// It might be useful in `StateReactSystem` where you know which category has been handled by the store for some performance.
        /// If you want to care, use `.Is`.
        /// </summary>
        public bool Type<ENUM>(ENUM actionEnum) where ENUM : struct, IConvertible
        => ETM.Category<ENUM>().FastCastToActionType(actionEnum) == enumActionTypeIndex;

        /// <summary>
        /// Each action type can be attached with multiple flags which are comparable cross-categories.
        /// `if` on this to check for a flag.
        /// </summary>
        public bool Flagged(string value)
        {
            int intFlag = ETM.StringFlagToInt(value);
            return flags.Contains(intFlag);
        }


        /// <summary>
        /// Match the key then unboxing from `object` to specified type, throws when the cast fail
        /// If `optional`, get a default value when payload key does not match. If the key match but the cast fail while `optional` you will still get a throw.
        /// </summary>
        public T GetPayload<T>(Enum payloadKey, bool optional = false) 
        {
            if (payload.TryGetValue(payloadKey, out object grab))
            {
                //Still can throw.
                return (T)grab;
            }
            else
            {
                return optional ? default(T)
                : throw new System.InvalidCastException($"There is no payload in {this} that match the key {payloadKey}.");
            }
        }

        public (T1, T2) GetPayload<T1, T2>(Enum pk1, Enum pk2, (bool, bool) optionals = default)
        => (
            GetPayload<T1>(pk1, optionals.Item1),
            GetPayload<T2>(pk2, optionals.Item2)
        );

        public (T1, T2, T3) GetPayload<T1, T2, T3>(Enum pk1, Enum pk2, Enum pk3, (bool, bool, bool) optionals = default)
        => (
            GetPayload<T1>(pk1, optionals.Item1),
            GetPayload<T2>(pk2, optionals.Item2),
            GetPayload<T3>(pk3, optionals.Item3)
        );

        public (T1, T2, T3, T4) GetPayload<T1, T2, T3, T4>(Enum pk1, Enum pk2, Enum pk3, Enum pk4, (bool, bool, bool, bool) optionals = default)
        => (
            GetPayload<T1>(pk1, optionals.Item1),
            GetPayload<T2>(pk2, optionals.Item2),
            GetPayload<T3>(pk3, optionals.Item3),
            GetPayload<T4>(pk4, optionals.Item4)
        );

        /// <summary>
        /// Unboxing from `object` to type T. Only returns true if payload is there and the cast to type T success.
        /// Useful when you want to check on the payload before other fields, or when it is possible to contains a payload or not.
        /// </summary>
        public bool HasPayload<T>(Enum payloadKey, out T castedPayload)
        {
            if (payload.TryGetValue(payloadKey, out object grab))
            {
                if (typeof(T).IsAssignableFrom(grab.GetType()))
                {
                    castedPayload = (T)grab;
                    return true;
                }
            }
            castedPayload = default(T);
            return false;
        }

        /// <summary>
        /// Use reflection on enum type to get a readable string.
        /// </summary>
        public string CategoryName => ETM.GetFullNameFromIndex(enumCategoryIndex);

        /// <summary>
        /// Use reflection on enum type to get a readable string.
        /// </summary>
        public string ActionTypeName => ETM.GetValueNameFromIndex(enumCategoryIndex, enumActionTypeIndex);
    }
}