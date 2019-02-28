using System;
using UnityEngine;
using Unity.Entities;
using UnityEditor;

namespace E7.EnumDispatcher
{
    public static class DispatchDebugger
    {
        /// <summary>
        /// Use `GUILayout` and `EditorGUILayout` to draw dispatcher debugger tools based on one category of action.
        /// </summary>
        public static void DrawDebuggerDropdown<ENUM>() where ENUM : struct, IConvertible
        {
            var ETM = EnumTypeManager.Singleton;
            bool unusable = ETM == null;
            // if(ETM == null) ETM = GetEditorWorld().GetOrCreateManager<EnumTypeManager>();

            EditorGUI.BeginDisabledGroup(unusable);

            Type enumType = ETM.Category<ENUM>().GetTypeOfEnum();

            var names = unusable ? new string[0] : ETM.Category<ENUM>().GetNames();
            var values = unusable ? new ENUM[0] : (ENUM[])Enum.GetValues(enumType);
            var niceName = unusable ? "---" : ETM.Category<ENUM>().FullNiceName();

            GUILayout.BeginHorizontal();
            GUILayout.Label(niceName);
            EditorStruct<ENUM>.selectedEnum = EditorGUILayout.Popup(EditorStruct<ENUM>.selectedEnum, names);
            if (GUILayout.Button(nameof(Dispatcher.Dispatch)))
            {
                Dispatcher.Dispatch<ENUM>(values[EditorStruct<ENUM>.selectedEnum]);
            }
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private struct EditorStruct<ENUM>
        {
            internal static int selectedEnum;
        }

        /// <summary>
        /// Use `GUILayout` to draw dispatcher debugger tools based on one category of action.
        /// </summary>
        public static void DrawDebuggerMatrix<ENUM>(int column = 3, int buttonHeight = 13) where ENUM : struct, IConvertible
        {
            var ETM = EnumTypeManager.Singleton;
            bool unusable = ETM == null;
            // var ETM = World.Active?.GetOrCreateManager<EnumTypeManager>();
            // bool unusable = ETM == null;
            // if(ETM == null) ETM = GetEditorWorld().GetOrCreateManager<EnumTypeManager>();

            EditorGUI.BeginDisabledGroup(unusable);

            GUIStyle btStyle = new GUIStyle(GUI.skin.button);
            btStyle.fontSize = (int)(buttonHeight / 1.5f);

            var names = unusable ? new string[0] : ETM.Category<ENUM>().GetNames();
            var values = unusable ? new ENUM[0] : (ENUM[])Enum.GetValues(ETM.Category<ENUM>().GetTypeOfEnum());
            var niceName = unusable ? "---" : ETM.Category<ENUM>().FullNiceName();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(niceName);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            Rect labelRect = GUILayoutUtility.GetLastRect();

            GUI.Box(labelRect, "");

            for (int i = 0; i < names.Length; i++)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(buttonHeight));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                Rect r = GUILayoutUtility.GetLastRect();
                r.width /= column;

                for (int j = 0; j < column && i < names.Length; j++)
                {
                    if (GUI.Button(r, names[i], btStyle))
                    {
                        Dispatcher.Dispatch(values[i]);
                    }
                    r.x += r.width;
                    i++;
                }
                GUILayout.BeginHorizontal(GUILayout.Height(1));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
