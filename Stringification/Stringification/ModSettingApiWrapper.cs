using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Duckov.Modding;
using UnityEngine;

namespace Stringification
{
    /// <summary>
    /// ModSetting API Wrapper for Stringification
    /// Stringification 的 ModSetting API 包装器
    /// Based on ModSettingAPI.cs from ModSetting-master
    /// 基于 ModSetting-master 中的 ModSettingAPI.cs
    /// </summary>
    public static class ModSettingApiWrapper
    {
        private const string ADD_KEYBINDING_WITH_DEFAULT = "AddKeybindingWithDefault";
        private const string GET_SAVED_VALUE = "GetSavedValue";
        private const string HAS_CONFIG = "HasConfig";
        
        public const string MOD_SETTING_NAME = "ModSetting";
        private const string TYPE_NAME = "ModSetting.ModBehaviour";
        
        private static Type? modBehaviourType;
        private static ModInfo modInfo;
        public static bool IsInit { get; private set; }

        // Cache delegates to avoid repeated reflection
        // 缓存委托以避免重复反射
        private static Dictionary<string, Delegate> methodCache = new Dictionary<string, Delegate>();

        /// <summary>
        /// Initialize the API
        /// 初始化 API
        /// </summary>
        /// <param name="info">Mod info / 模组信息</param>
        /// <returns>True if successful / 如果成功则返回 True</returns>
        public static bool Init(ModInfo info)
        {
            if (IsInit) return true;
            
            modInfo = info;
            modBehaviourType = FindTypeInAssemblies(TYPE_NAME);
            
            if (modBehaviourType == null)
            {
                Debug.LogWarning($"[Stringification] ModSetting not found. Configuration will be disabled.");
                return false;
            }

            IsInit = true;
            Debug.Log($"[Stringification] ModSetting initialized successfully.");
            return true;
        }

        /// <summary>
        /// Add a keybinding with default value
        /// 添加带有默认值的按键绑定
        /// </summary>
        public static bool AddKeybinding(string key, string description, KeyCode keyCode, KeyCode defaultKeyCode, Action<KeyCode>? onValueChange = null)
        {
            if (!Available(key)) return false;
            
            // Signature: void AddKeybindingWithDefault(ModInfo modInfo, string key, string description, KeyCode keyCode, KeyCode defaultKeyCode, Action<KeyCode> onValueChange)
            Type delegateType = typeof(Action<ModInfo, string, string, KeyCode, KeyCode, Action<KeyCode>>);
            
            return InvokeMethod(
                ADD_KEYBINDING_WITH_DEFAULT,
                ADD_KEYBINDING_WITH_DEFAULT,
                new object?[] { modInfo, key, description, keyCode, defaultKeyCode, onValueChange },
                delegateType
            );
        }

        /// <summary>
        /// Get saved value
        /// 获取保存的值
        /// </summary>
        public static bool GetSavedValue<T>(string key, out T value)
        {
            value = default!;
            if (!Available(key)) return false;

            MethodInfo? methodInfo = GetStaticPublicMethodInfo(GET_SAVED_VALUE);
            if (methodInfo == null) return false;

            MethodInfo genericMethod = methodInfo.MakeGenericMethod(typeof(T));
            
            // Prepare parameters array (out parameter needs special handling)
            // 准备参数数组（out 参数需要特殊处理）
            object?[] parameters = new object?[] { modInfo, key, null };
            
            try
            {
                bool result = (bool)genericMethod.Invoke(null, parameters)!;
                // Get value from out parameter
                // 从 out 参数获取值
                if (parameters[2] != null)
                {
                    value = (T)parameters[2]!;
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stringification] Failed to invoke GetSavedValue: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add a slider setting (float)
        /// 添加滑块设置（浮点数）
        /// </summary>
        public static bool AddSlider(string key, string description, float value, float min, float max, Action<float>? onValueChange = null)
        {
            if (!Available(key)) return false;
            
            // Signature: void AddSlider(ModInfo modInfo, string key, string description, float defaultValue, Vector2 sliderRange, Action<float> onValueChange, int decimalPlaces, int characterLimit)
            Type delegateType = typeof(Action<ModInfo, string, string, float, Vector2, Action<float>, int, int>);
            Type[] paramTypes = new Type[] { typeof(ModInfo), typeof(string), typeof(string), typeof(float), typeof(Vector2), typeof(Action<float>), typeof(int), typeof(int) };

            return InvokeMethod(
                "AddSliderFloat",
                "AddSlider",
                new object?[] { modInfo, key, description, value, new Vector2(min, max), onValueChange, 1, 5 },
                delegateType,
                paramTypes
            );
        }

        /// <summary>
        /// Add a slider setting (int)
        /// 添加滑块设置（整数）
        /// </summary>
        public static bool AddSlider(string key, string description, int value, int min, int max, Action<int>? onValueChange = null)
        {
            if (!Available(key)) return false;
            
            // Signature: void AddSlider(ModInfo modInfo, string key, string description, int defaultValue, int minValue, int maxValue, Action<int> onValueChange, int characterLimit)
            Type delegateType = typeof(Action<ModInfo, string, string, int, int, int, Action<int>, int>);
            Type[] paramTypes = new Type[] { typeof(ModInfo), typeof(string), typeof(string), typeof(int), typeof(int), typeof(int), typeof(Action<int>), typeof(int) };
            
            return InvokeMethod(
                "AddSliderInt",
                "AddSlider",
                new object?[] { modInfo, key, description, value, min, max, onValueChange, 5 },
                delegateType,
                paramTypes
            );
        }

        /// <summary>
        /// Add a toggle setting
        /// 添加开关设置
        /// </summary>
        public static bool AddToggle(string key, string description, bool enable, Action<bool>? onValueChange = null)
        {
            if (!Available(key)) return false;
            
            // Signature: void AddToggle(ModInfo modInfo, string key, string description, bool enable, Action<bool> onValueChange)
            Type delegateType = typeof(Action<ModInfo, string, string, bool, Action<bool>>);
            
            return InvokeMethod(
                "AddToggle",
                "AddToggle",
                new object?[] { modInfo, key, description, enable, onValueChange },
                delegateType
            );
        }

        private static bool Available(string key)
        {
            return IsInit && modInfo.displayName != null && modInfo.name != null && key != null;
        }

        private static Type? FindTypeInAssemblies(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type? type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }

        private static MethodInfo? GetStaticPublicMethodInfo(string methodName, Type[]? paramTypes = null)
        {
            if (!IsInit || modBehaviourType == null) return null;
            if (paramTypes == null)
            {
                try 
                {
                    return modBehaviourType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                }
                catch (AmbiguousMatchException)
                {
                    Debug.LogError($"[Stringification] Ambiguous match for method {methodName}. Please specify parameter types.");
                    return null;
                }
            }
            else
            {
                return modBehaviourType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, paramTypes, null);
            }
        }

        private static bool InvokeMethod(string cacheKey, string methodName, object?[] parameters, Type delegateType, Type[]? paramTypes = null)
        {
            if (!methodCache.ContainsKey(cacheKey))
            {
                MethodInfo? method = GetStaticPublicMethodInfo(methodName, paramTypes);
                if (method == null)
                {
                    Debug.LogError($"[Stringification] Method {methodName} not found in ModSetting.");
                    return false;
                }
                // Create delegate
                // 创建委托
                try
                {
                    methodCache[cacheKey] = Delegate.CreateDelegate(delegateType, method);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Stringification] Failed to create delegate for {methodName}: {ex.Message}");
                    return false;
                }
            }

            try
            {
                methodCache[cacheKey].DynamicInvoke(parameters);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stringification] Failed to invoke {methodName}: {ex.Message}");
                return false;
            }
        }
    }
}
