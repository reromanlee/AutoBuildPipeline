#if UNITY_6000_0_OR_NEWER
using System;
using System.Reflection;
#else
using UnityEditor;
#endif

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Reads and writes Unity's Android native debug symbols setting across Unity 2021.3, 2022.3 and Unity 6
    /// without a compile-time dependency on the Android build support module.
    /// </summary>
    internal static class AndroidDebugSymbolsUtility
    {
#if UNITY_6000_0_OR_NEWER
        private const string DebugSymbolsTypeName = "UnityEditor.Android.UserBuildSettings+DebugSymbols";
        private static bool _isLevelPropertyResolved;
        private static PropertyInfo _levelProperty;
#endif

        /// <summary>
        /// Reads the current Android debug symbol level configured in the editor.
        /// Returns false on Unity 6 when the Android build support module is not installed.
        /// </summary>
        public static bool TryRead(out AndroidDebugSymbols symbols)
        {
#if UNITY_6000_0_OR_NEWER
            PropertyInfo levelProperty = GetLevelProperty();
            if (levelProperty == null)
            {
                symbols = AndroidDebugSymbols.Off;
                return false;
            }

            object value = levelProperty.GetValue(null);
            symbols = FromUnity6Level(value);
            return true;
#else
            symbols = FromLegacy(EditorUserBuildSettings.androidCreateSymbols);
            return true;
#endif
        }

        /// <summary>
        /// Sets Unity's Android debug symbol level, writing only on change to avoid dirtying settings unnecessarily.
        /// On Unity 6, leaves <c>DebugSymbols.format</c> untouched and returns false when the Android module is absent.
        /// </summary>
        public static bool TryWrite(AndroidDebugSymbols symbols)
        {
#if UNITY_6000_0_OR_NEWER
            PropertyInfo levelProperty = GetLevelProperty();
            if (levelProperty == null)
            {
                return false;
            }

            object currentValue = levelProperty.GetValue(null);
            if (FromUnity6Level(currentValue) == symbols)
            {
                return true;
            }

            object targetValue = ToUnity6Level(levelProperty.PropertyType, symbols);
            levelProperty.SetValue(null, targetValue);
            return true;
#else
            AndroidCreateSymbols target = ToLegacy(symbols);
            if (EditorUserBuildSettings.androidCreateSymbols != target)
            {
                EditorUserBuildSettings.androidCreateSymbols = target;
            }

            return true;
#endif
        }

#if UNITY_6000_0_OR_NEWER
        private static PropertyInfo GetLevelProperty()
        {
            if (_isLevelPropertyResolved)
            {
                return _levelProperty;
            }

            _isLevelPropertyResolved = true;
            Type debugSymbolsType = FindType(DebugSymbolsTypeName);
            _levelProperty = debugSymbolsType?.GetProperty("level", BindingFlags.Public | BindingFlags.Static);
            return _levelProperty;
        }

        private static Type FindType(string fullName)
        {
            Type type = Type.GetType(fullName + ", UnityEditor.Android.Extensions");
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static AndroidDebugSymbols FromUnity6Level(object value)
        {
            if (value == null)
            {
                return AndroidDebugSymbols.Off;
            }

            switch (value.ToString())
            {
                case "SymbolTable":
                {
                    return AndroidDebugSymbols.Public;
                }
                case "Full":
                {
                    return AndroidDebugSymbols.Debugging;
                }
                default:
                {
                    return AndroidDebugSymbols.Off;
                }
            }
        }

        private static object ToUnity6Level(Type enumType, AndroidDebugSymbols symbols)
        {
            string name = symbols switch
            {
                AndroidDebugSymbols.Public => "SymbolTable",
                AndroidDebugSymbols.Debugging => "Full",
                _ => "None"
            };

            return Enum.Parse(enumType, name);
        }
#else
        private static AndroidDebugSymbols FromLegacy(AndroidCreateSymbols symbols)
        {
            switch (symbols)
            {
                case AndroidCreateSymbols.Public:
                {
                    return AndroidDebugSymbols.Public;
                }
                case AndroidCreateSymbols.Debugging:
                {
                    return AndroidDebugSymbols.Debugging;
                }
                default:
                {
                    return AndroidDebugSymbols.Off;
                }
            }
        }

        private static AndroidCreateSymbols ToLegacy(AndroidDebugSymbols symbols)
        {
            switch (symbols)
            {
                case AndroidDebugSymbols.Public:
                {
                    return AndroidCreateSymbols.Public;
                }
                case AndroidDebugSymbols.Debugging:
                {
                    return AndroidCreateSymbols.Debugging;
                }
                default:
                {
                    return AndroidCreateSymbols.Disabled;
                }
            }
        }
#endif
    }
}
