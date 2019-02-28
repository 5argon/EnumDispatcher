using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace E7.EnumDispatcher
{
    /// <summary>
    /// A database to make enum-as-action works.
    /// Stole the idea from Unity ECS's TypeManager. Haha!
    /// </summary>
    [DisableAutoCreation]
    public class EnumTypeManager : ComponentSystem
    {
        const string etmWorldName = "Izumi ETM World";
        static World etmWorld;
        static EnumTypeManager singletonEtm;

        /// <summary>
        /// To not litter your game's world with utility systems.
        /// </summary>
        public static EnumTypeManager Singleton
        {
            get
            {
#if UNITY_EDITOR
                //On exiting play mode this is from F T T T -> F F T F
                //Debug.Log($"{EditorApplication.isPlayingOrWillChangePlaymode} && !{EditorApplication.isPlaying} && {etmWorld != null} && {etmWorld?.IsCreated}");
                // if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                // {
                //     return null;
                // }
#endif
                if (etmWorld == null || etmWorld.IsCreated == false)
                {
                    etmWorld = new World(etmWorldName);
                    singletonEtm = etmWorld.GetOrCreateManager<EnumTypeManager>();
                    PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown, 10000);
                }
                return singletonEtm;
            }
        }

        //If you disabled default world you will be missing this auto dispose
        //that Unity used in Entities lib.
        static void DomainUnloadShutdown()
        {
            World.DisposeAllWorlds();
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
        }

        //One system will be created per action category. This is to make things static-like while not really static. (Static in a world)
        public EnumTypeManager<T> Category<T>() where T : struct, IConvertible
        => World.GetOrCreateManager<EnumTypeManager<T>>();

        protected override void OnUpdate() { }
        protected override void OnCreateManager()
        {
            this.Enabled = false;
        }

        internal int GetCategoryIndex<ENUM>() where ENUM : struct, IConvertible
        {
            //Debug.Log($"Looking up {typeof(ENUM).FullName} {StaticLookup<ENUM>.typeIndex} ");
            var cat = Category<ENUM>();
            if (cat.typeIndex == default(int))
            {
                runningEnumTypeIndex++;
                cat.typeIndex = runningEnumTypeIndex;
                typeDict.Add(runningEnumTypeIndex, typeof(ENUM));
                //Debug.Log($"Added category {typeof(ENUM).FullName} as {runningEnumTypeIndex}");
            }
            return cat.typeIndex;
        }

        internal string GetFullNameFromIndex(int index) => GetTypeFromIndex(index).FullName;
        internal string GetValueNameFromIndex(int index, int valueIndex) => Enum.GetName(GetTypeFromIndex(index), valueIndex);

        /// <summary>
        /// Dictionary-cached `typeof` of that `index`.
        /// </summary>
        internal Type GetTypeFromIndex(int index)
        {
            if (typeDict.TryGetValue(index, out Type t))
            {
                return t;
            }
            else
            {
                throw new ArgumentException($"Type index {index} is not yet indexed. Please ensure the index argument was made from `GetTypeIndex<EnumType>`.");
            }
        }

        internal int StringFlagToInt(string stringFlag)
        {
            if (stringFlagToIntFlagDict.TryGetValue(stringFlag, out int intFlag))
            {
                return intFlag;
            }
            else
            {
                runningFlagIndex++;
                stringFlagToIntFlagDict.Add(stringFlag, runningFlagIndex);
                intFlagToStringFlagDict.Add(runningFlagIndex, stringFlag);
                return runningFlagIndex;
            }
        }

        internal string IntFlagToString(int intFlag)
        {
            if (intFlagToStringFlagDict.TryGetValue(intFlag, out string stringFlag))
            {
                return stringFlag;
            }
            else
            {
                throw new ArgumentException($"{intFlag} is not corresponding to any previously registered string flag");
            }
        }

        private int runningEnumTypeIndex = default(int);
        private Dictionary<int, Type> typeDict = new Dictionary<int, Type>();

        private int runningFlagIndex = default(int);
        //Hash collision will be handled by dictionary.
        private Dictionary<string, int> stringFlagToIntFlagDict = new Dictionary<string, int>();
        private Dictionary<int, string> intFlagToStringFlagDict = new Dictionary<int, string>();
    }

    /// <summary>
    /// A utility system holding cached information for a specific type of enum.
    /// </summary>
    //Because it contains generic, it won't be auto created to the world.
    public class EnumTypeManager<T> : ComponentSystem
    where T : struct, IConvertible
    {
        EnumTypeManager ETM;
        protected override void OnCreateManager()
        {
            ETM = World.GetOrCreateManager<EnumTypeManager>();
            this.Enabled = false;
        }

        bool castMapsGenerated;
        protected override void OnDestroyManager()
        {
            if (castMapsGenerated)
            {
                fastCastDictionary.Dispose();
                foreach (var na in flagsDictionary.Values)
                {
                    na.Dispose();
                }
            }
        }

        protected override void OnUpdate() { }

        internal string[] names;
        internal int typeIndex;
        internal Type type;

        private NativeHashMap<int, T> fastCastDictionary;
        internal NativeHashMap<int, T> FastCastDictionary
        {
            get
            {
                if (!castMapsGenerated) GenerateAllCastMaps();
                return fastCastDictionary;
            }
        }

        //enum does not implement IEquatable lol
        private Dictionary<T, int> fastCastBackDictionary;
        internal Dictionary<T, int> FastCastBackDictionary
        {
            get
            {
                if (!castMapsGenerated) GenerateAllCastMaps();
                return fastCastBackDictionary;
            }
        }

        //We would like to be able to hand those flags directly to C# jobs.
        //So we are storing these native containers statically in the first place then share to everyone.
        private Dictionary<int, NativeArray<int>> flagsDictionary;
        internal Dictionary<int, NativeArray<int>> FlagsDictionary
        {
            get
            {
                if (!castMapsGenerated) GenerateAllCastMaps();
                return flagsDictionary;
            }
        }

        /// <summary>
        /// Possible to call this manually to cache enums even before the first actual use.
        /// </summary>
        public void GenerateAllCastMaps()
        {
            var type = typeof(T);
            var values = (T[])(Enum.GetValues(type));
            var intValues = values.Select(x => Convert.ToInt32(x)).ToArray();
            fastCastDictionary = new NativeHashMap<int, T>(values.Length, Allocator.Persistent);
            fastCastBackDictionary = new Dictionary<T, int>(values.Length);
            flagsDictionary = new Dictionary<int, NativeArray<int>>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                fastCastDictionary.TryAdd(intValues[i], values[i]);
                fastCastBackDictionary.Add(values[i], intValues[i]);
            }

            for (int i = 0; i < values.Length; i++)
            {
                var memInfo = type.GetMember(values[i].ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(FAttribute), false);
                string[] getFlags = (attributes.Length > 0) ? ((FAttribute)attributes[0]).flags : new string[0];
                int[] intFlags = getFlags.Select(x => ETM.StringFlagToInt(x)).ToArray();

                NativeArray<int> naIntFlags = new NativeArray<int>(intFlags, Allocator.Persistent);
                flagsDictionary.Add(intValues[i], naIntFlags);
            }
            castMapsGenerated = true;
        }

        /// <summary>
        /// Cached `typeof(ENUM)`.
        /// </summary>
        public Type GetTypeOfEnum()
        {
            if (type == default(Type))
            {
                type = typeof(T);
            }
            return type;
        }

        /// <summary>
        /// Array of flags are statically cached for each action type.
        /// Each `DispatchAction` would then get a copy of these arrays by reference.
        /// </summary>
        internal NativeArray<int> GetFlags(T actionType)
        {
            if (FlagsDictionary.TryGetValue(FastCastToActionType(actionType), out NativeArray<int> flags))
            {
                return flags;
            }
            else
            {
                throw new System.Exception($"It should have cache everything in {typeof(T).Name}, why {actionType} not found?");
            }
        }

        /// <summary>
        /// Dictionary-cached int + generic to ENUM cast.
        /// </summary>
        internal T FastCastFromActionType(int enumIntValue)
        {
            if (FastCastDictionary.TryGetValue(enumIntValue, out T castedValue))
            {
                return castedValue;
            }
            else
            {
                throw new System.Exception($"It should have cache everything in {typeof(T).Name}, why {enumIntValue} not found?");
            }
        }

        /// <summary>
        /// Dictionary-cached enum + generic to int cast.
        /// </summary>
        internal int FastCastToActionType(T enumValue)
        {
            if (FastCastBackDictionary.TryGetValue(enumValue, out int castedValue))
            {
                //Debug.Log($"Fast casted {enumValue} to integer {castedValue}");
                return castedValue;
            }
            else
            {
                throw new System.Exception($"It should have cache everything in {typeof(T).Name}, why {enumValue} not found?");
            }
        }

        /// <summary>
        /// Cached enum names
        /// </summary>
        public string[] GetNames()
        {
            if (names == default(string[]))
            {
                names = Enum.GetNames(GetTypeOfEnum());
            }
            return names;
        }

        public string FullNiceName() => GetTypeOfEnum().FullName.Replace('+', '.');
    }
}