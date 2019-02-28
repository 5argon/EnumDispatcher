using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace E7.EnumDispatcher
{
    /// <summary>
    /// A <see cref="DispatchAction"> which works in a job.
    /// </summary>
    public struct JobDispatchAction 
    {
        public override string ToString() => $"JDA inner value : {categoryAndTypeIndex}";

        internal int2 categoryAndTypeIndex;
        //Flags was a string, in the job we use int representation bookkeeped by Izumi out of a job.
        //Do not Dispose! Everyone is sharing this memory!
        [ReadOnly] internal NativeArray<int> flags;

        internal JobDispatchAction(int category, int actionType, NativeArray<int> flags)
        {
            this.categoryAndTypeIndex = new int2(category, actionType);
            this.flags = flags;
        }

        /// <summary>
        /// Works like `Category` of `DispatchAction` but requires baked `ActionCategory` from outside of the job.
        /// The baked `ActionCategory` includes `NativeHashMap` for fast casting to `out` variable from this method.
        /// </summary>
        public bool Category<ENUM>(ActionCategory<ENUM> ac, out ENUM outEnum)
        where ENUM : struct
        {
            if (categoryAndTypeIndex.x == ac.categoryIndex)
            {
                outEnum = As(ac);
                return true;
            }
            else
            {
                outEnum = default(ENUM);
                return false;
            }
        }

        /// <summary>
        /// Works like `Is` of `DispatchAction` but requires baked `ActionExact`, an enum representation from outside of the job.
        /// </summary>
        public bool Is(ActionExact ac)
        {
            if(ac.categoryAndTypeIndex.Equals(default(int2)))
            {
                throw new ArgumentException($"ActionExact's content is empty. Did you schedule a job without assigning the field's value?");
            }
            return categoryAndTypeIndex.Equals(ac.categoryAndTypeIndex);
        }

        /// <summary>
        /// If you have checked the category outside the job, then you can `switch` on the `As` directly in-job.
        /// </summary>
        public ENUM As<ENUM>(ActionCategory<ENUM> ac)
        where ENUM : struct
        {
            if (ac.fastConvert.TryGetValue(categoryAndTypeIndex.y, out ENUM casted))
            {
                return casted;
            }
            else
            {
                throw new System.Exception($"Type index {categoryAndTypeIndex.y} does not belong in category {typeof(ENUM).FullName}");
            }
        }

        /// <summary>
        /// In a job you cannot use `string`, so you need to bake `ActionFlag` from outside for use in-job.
        /// </summary>
        public bool Flagged(ActionFlag flag) 
        {
            if(flag.flagValue == default(int))
            {
                throw new ArgumentException($"ActionFlag's content is empty. Did you schedule a job without assigning the field's value?");
            }
            return flags.Contains(flag.flagValue);
        }
    }
}