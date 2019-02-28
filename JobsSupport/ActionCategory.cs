using System;
using Unity.Collections;

namespace E7.EnumDispatcher
{
    /// <summary>
    /// It make checking for an action's category works in a job.
    /// </summary>
    public struct ActionCategory<ENUM> : IDisposable
    where ENUM : struct
    {
        internal int categoryIndex;
        [ReadOnly] internal NativeHashMap<int, ENUM> fastConvert;

        public void Dispose()
        {
            fastConvert.Dispose();
        }
    }
}