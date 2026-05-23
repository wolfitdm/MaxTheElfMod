using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MaxTheElfMod
{
    [BepInPlugin("com.wolfitdm.MaxTheElfMod", "MaxTheElfMod Plugin", "1.0.0.0")]
    public class MaxTheElfMod : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private static ConfigEntry<bool> configSkipGameOverPlay = null;
        private static ConfigEntry<bool> configResetCorruptionLevelAfterFuck = null;
        private static ConfigEntry<KeyCode> configSkipGameOverPlayKey = null;
        private static ConfigEntry<KeyCode> configResetCorruptionLevelAfterFuckKey = null;

        private static bool skipGameOverPlay = false;
        private static bool resetCorruptionLevelAfterFuck = false;
        private static KeyCode skipGameOverPlayKey = KeyCode.F8;
        private static KeyCode resetCorruptionLevelAfterFuckKey = KeyCode.F9;

        public MaxTheElfMod()
        {
        }

        public static Type MyGetType(string originalClassName)
        {
            return Type.GetType(originalClassName + ",Assembly-CSharp");
        }

        private static string pluginKey = "General.Toggles";

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;

            configSkipGameOverPlay = Config.Bind(pluginKey,
                                                         "SkipGameOverPlay",
                                                          true,
                                                         "Skip GameOver Play ? default = true also yes, false = no");

            configResetCorruptionLevelAfterFuck = Config.Bind(pluginKey,
                                             "ResetCorruptionLevelAfterFuck",
                                              true,
                                             "Reset Corruption Level After Fuck ? default = true also yes, false = no");

            configSkipGameOverPlayKey = Config.Bind(pluginKey,
                                             "SkipGameOverPlayKeyCode",
                                              KeyCode.F8,
                                             "Toggle Skip GameOver Play, default F8");

            configResetCorruptionLevelAfterFuckKey = Config.Bind(pluginKey,
                                             "ResetCorruptionLevelAfterFuckKeyCode",
                                              KeyCode.F9,
                                             "Toggle Reset Corruption Level After Fuck, default F9");

            skipGameOverPlay = configSkipGameOverPlay.Value;
            resetCorruptionLevelAfterFuck = configResetCorruptionLevelAfterFuck.Value;
            skipGameOverPlayKey = configSkipGameOverPlayKey.Value;
            resetCorruptionLevelAfterFuckKey = configSkipGameOverPlayKey.Value;


            PatchAllHarmonyMethods();

            Logger.LogInfo($"Plugin MaxTheElfMod BepInEx is loaded!");
        }
        private void OnGUI()
        {
        }

        private void Update()
        {
            if (Input.GetKeyUp(skipGameOverPlayKey))
            {
                skipGameOverPlay = !skipGameOverPlay;
                configSkipGameOverPlay.Value = skipGameOverPlay;

                if (skipGameOverPlay)
                {
                    Logger.LogInfo("skipGameOverPlay: on");
                } else
                {
                    Logger.LogInfo("skipGameOverPlay: off");
                }
            }

            if (Input.GetKeyUp(resetCorruptionLevelAfterFuckKey))
            {
                resetCorruptionLevelAfterFuck = !resetCorruptionLevelAfterFuck;
                configResetCorruptionLevelAfterFuck.Value = resetCorruptionLevelAfterFuck;

                if (resetCorruptionLevelAfterFuck)
                {
                    Logger.LogInfo("resetCorruptionLevelAfterFuck: on");
                }
                else
                {
                    Logger.LogInfo("resetCorruptionLevelAfterFuck: off");
                }
            }
        }

        public static bool GameOver(object __instance) {
            
            if (!skipGameOverPlay)
            {
                return true;
            } 

            if (gameplayManager == null)
            {
                GameplayManager __GM = Traverse.Create(__instance).Field("GM").GetValue<GameplayManager>();
                gameplayManager = __GM;
            }

            if (resetCorruptionLevelAfterFuck)
                gameplayManager.SetCorruptionLevel(0);

            return false; 
        }

        public static bool GameOverGM(bool win, int enemyIdx, int overrideLevelIndex, bool masturbating, bool skipWinScreens, object __instance)
        {
            if (!skipGameOverPlay)
            {
                return true;
            }

            if (gameplayManager == null)
            {
                gameplayManager = (GameplayManager)__instance;
            }

            if (resetCorruptionLevelAfterFuck)
                gameplayManager.SetCorruptionLevel(0);

            return false;
        }

        private static MaxController maxController = null;
        private static GameplayManager gameplayManager = null;
        public static bool UpdateMaxController(object __instance)
        {
            MaxController _this = (MaxController) __instance;

            if (gameplayManager == null)
            {
                gameplayManager = _this.GameplayManagerComponent;
            }

            if (resetCorruptionLevelAfterFuck)
                gameplayManager.SetCorruptionLevel(0);

            return true;
        }

        public static bool UpdateMaxControllerRevamp(object __instance)
        {
            if (gameplayManager == null)
            {
                GameplayManager __GM = Traverse.Create(__instance).Field("GM").GetValue<GameplayManager>();
                gameplayManager = __GM;
            }

            if (resetCorruptionLevelAfterFuck)
                gameplayManager.SetCorruptionLevel(0);

            return true;
        }
        public static void PatchAllHarmonyMethods()
        {
            if (skipGameOverPlay)
            {
                try
                {
                    PatchHarmonyMethodUnity(typeof(MaxControllerRevamp), "GameOver", "GameOver", true, false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.ToString());
                }

                try
                {
                    PatchHarmonyMethodUnity(typeof(GameplayManager), "GameOver", "GameOverGM", true, false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.ToString());
                }
            }

            if (!resetCorruptionLevelAfterFuck)
            {
                return;
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(MaxControllerRevamp), "Update", "UpdateMaxControllerRevamp", true, false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(MaxController), "Update", "UpdateMaxController", true, false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }
        public static void PatchHarmonyMethodUnity(Type originalClass, string originalMethodName, string patchedMethodName, bool usePrefix, bool usePostfix, Type[] parameters = null)
        {
            string uniqueId = "com.wolfitdm.MaxTheElfMod";
            Type uniqueType = typeof(MaxTheElfMod);

            // Create a new Harmony instance with a unique ID
            var harmony = new Harmony(uniqueId);

            if (originalClass == null)
            {
                Logger.LogInfo($"GetType originalClass == null");
                return;
            }

            MethodInfo patched = null;

            try
            {
                patched = AccessTools.Method(uniqueType, patchedMethodName);
            }
            catch (Exception ex)
            {
                patched = null;
            }

            if (patched == null)
            {
                Logger.LogInfo($"AccessTool.Method patched {patchedMethodName} == null");
                return;

            }

            // Or apply patches manually
            MethodInfo original = null;

            try
            {
                if (parameters == null)
                {
                    original = AccessTools.Method(originalClass, originalMethodName);
                }
                else
                {
                    original = AccessTools.Method(originalClass, originalMethodName, parameters);
                }
            }
            catch (AmbiguousMatchException ex)
            {
                Type[] nullParameters = new Type[] { };
                try
                {
                    if (patched == null)
                    {
                        parameters = nullParameters;
                    }

                    ParameterInfo[] parameterInfos = patched.GetParameters();

                    if (parameterInfos == null || parameterInfos.Length == 0)
                    {
                        parameters = nullParameters;
                    }

                    List<Type> parametersN = new List<Type>();

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        ParameterInfo parameterInfo = parameterInfos[i];

                        if (parameterInfo == null)
                        {
                            continue;
                        }

                        if (parameterInfo.Name == null)
                        {
                            continue;
                        }

                        if (parameterInfo.Name.StartsWith("__"))
                        {
                            continue;
                        }

                        Type type = parameterInfos[i].ParameterType;

                        if (type == null)
                        {
                            continue;
                        }

                        parametersN.Add(type);
                    }

                    parameters = parametersN.ToArray();
                }
                catch (Exception ex2)
                {
                    parameters = nullParameters;
                }

                try
                {
                    original = AccessTools.Method(originalClass, originalMethodName, parameters);
                }
                catch (Exception ex2)
                {
                    original = null;
                }
            }
            catch (Exception ex)
            {
                original = null;
            }

            if (original == null)
            {
                Logger.LogInfo($"AccessTool.Method original {originalMethodName} == null");
                return;
            }

            HarmonyMethod patchedMethod = new HarmonyMethod(patched);
            var prefixMethod = usePrefix ? patchedMethod : null;
            var postfixMethod = usePostfix ? patchedMethod : null;

            harmony.Patch(original,
                prefix: prefixMethod,
                postfix: postfixMethod);
        }

    }
}
