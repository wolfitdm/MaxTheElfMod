using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MaxTheElfMod
{
    public class AudioSourceHolder
    {
        public float audioVolume = 0;
        public bool muteOn = false;
        public bool playOn = false;
        public bool pauseOn = false;
    }

    [BepInPlugin("com.wolfitdm.MaxTheElfMod", "MaxTheElfMod Plugin", "1.0.0.0")]
    public class MaxTheElfMod : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private static ConfigEntry<bool> configSkipGameOverPlay = null;
        private static ConfigEntry<bool> configResetCorruptionLevelAfterFuck = null;
        private static ConfigEntry<KeyCode> configSkipGameOverPlayKey = null;
        private static ConfigEntry<KeyCode> configResetCorruptionLevelAfterFuckKey = null;
        private static ConfigEntry<KeyCode> configOpenMenu = null;
        private static ConfigEntry<bool> configEnableMe = null;

        private static bool skipGameOverPlay = false;
        private static bool resetCorruptionLevelAfterFuck = false;
        private static bool enableMe = false;

        private static KeyCode skipGameOverPlayKey = KeyCode.R;
        private static KeyCode resetCorruptionLevelAfterFuckKey = KeyCode.T;
        private static KeyCode keyCodeOpenMenu = KeyCode.Z;

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

            configEnableMe = Config.Bind(pluginKey,
                                             "EnableMe",
                                              true,
                                             "Enable the mod, default = true, also yes, false = no");

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
                                              KeyCode.R,
                                             "Toggle Skip GameOver Play, default R");

            configResetCorruptionLevelAfterFuckKey = Config.Bind(pluginKey,
                                             "ResetCorruptionLevelAfterFuckKeyCode",
                                             KeyCode.T,
                                             "Toggle Reset Corruption Level After Fuck, default T");

            configOpenMenu = Config.Bind(pluginKey,
                                 "OpenMenuKeyCode",
                                 KeyCode.Z,
                                 "Key press to open the cheat menu, default Z");

            skipGameOverPlay = configSkipGameOverPlay.Value;
            resetCorruptionLevelAfterFuck = configResetCorruptionLevelAfterFuck.Value;

            skipGameOverPlayKey = configSkipGameOverPlayKey.Value;
            resetCorruptionLevelAfterFuckKey = configResetCorruptionLevelAfterFuckKey.Value;
            keyCodeOpenMenu = configOpenMenu.Value;
            enableMe = configEnableMe.Value;

            PatchAllHarmonyMethods();

            Logger.LogInfo($"Plugin MaxTheElfMod BepInEx is loaded!");
        }

        private static bool showMenu = false;
        private bool foldoutAudio = true;
        private Rect windowRect2 = new Rect(20, 20, 650, 625);
        private void OnGUI()
        {
            if (showMenu)
            {
                try
                {
                    windowRect2 = GUI.Window(0, windowRect2, DrawCheatWindow, "MaxTheEldModMenu");
                }
                catch (Exception ex)
                {
                }
            }
        }

        private bool EditorLikeFoldout(bool foldout, string title)
        {
            GUILayout.BeginHorizontal();
            string arrow = foldout ? "▼" : "▶";
            if (GUILayout.Button(arrow + " " + title, GUI.skin.label))
            {
                foldout = !foldout;
            }
            GUILayout.EndHorizontal();
            return foldout;
        }

        private Vector2 scrollPos;

        private void DrawCheatWindow(int windowID)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            toggleButton("skipGameOverPlay", skipGameOverPlay, () =>
            {
                skipGameOverPlay = !skipGameOverPlay;
                configSkipGameOverPlay.Value = skipGameOverPlay;

                if (skipGameOverPlay)
                {
                    Logger.LogInfo("skipGameOverPlay: on");
                    if (restartGameSkipGameOverPlay)
                    {
                        Logger.LogInfo("You must restart the game to use skipGameOverPlay");
                    }
                }
                else
                {
                    Logger.LogInfo("skipGameOverPlay: off");
                }
            });

            toggleButton("resetCorruptionLevelAfterFuck", resetCorruptionLevelAfterFuck, () =>
            {
                resetCorruptionLevelAfterFuck = !resetCorruptionLevelAfterFuck;
                configResetCorruptionLevelAfterFuck.Value = resetCorruptionLevelAfterFuck;

                if (resetCorruptionLevelAfterFuck)
                {
                    Logger.LogInfo("resetCorruptionLevelAfterFuck: on");
                    if (restartGameResetCorruptionLevelAfterFuck)
                    {
                        Logger.LogInfo("You must restart the game to use resetCorruptionLevelAfterFuck");
                    }
                }
                else
                {
                    Logger.LogInfo("resetCorruptionLevelAfterFuck: off");
                }
            });

            foldoutAudio = EditorLikeFoldout(foldoutAudio, "Audio");

            if (foldoutAudio)
            {
                foreach (string key in audioSources.Keys)
                {
                    AudioSource audioSource = audioSources[key];

                    if (audioSource == null)
                    {
                        continue;
                    }

                    if (!audioSourcesHolder.ContainsKey(key))
                    {
                        audioSourcesHolder.Add(key, new AudioSourceHolder());
                        audioSourcesHolder[key].audioVolume = 0;
                        audioSourcesHolder[key].muteOn = audioSource.mute;
                        audioSourcesHolder[key].pauseOn = false;
                        audioSourcesHolder[key].playOn = audioSource.isPlaying;
                    }

                    GUILayout.BeginVertical("box", GUILayout.Width(620));

                    GUILayout.Label(key);
                    audioSourcesHolder[key].audioVolume = slider("volume", audioSourcesHolder[key].audioVolume, 0, 10);
                    GUILayout.BeginHorizontal();


                    audioSourcesHolder[key].playOn = audioSource.isPlaying;
                    string play = audioSourcesHolder[key].playOn ? "Stop" : "Play";

                    if (GUILayout.Button(play, GUILayout.Width(100)))
                    {
                        if (audioSourcesHolder[key].playOn)
                        {
                            audioSource.UnPause();
                            audioSource.Stop();
                            audioSourcesHolder[key].pauseOn = false;
                            audioSourcesHolder[key].playOn = false;
                        }
                        else
                        {
                            audioSource.UnPause();
                            audioSource.Play();
                            audioSourcesHolder[key].pauseOn = false;
                            audioSourcesHolder[key].playOn = audioSource.isPlaying;
                        }
                    }

                    string pause = audioSourcesHolder[key].pauseOn ? "UnPause" : "Pause";

                    if (GUILayout.Button(pause, GUILayout.Width(100)))
                    {
                        if (audioSourcesHolder[key].pauseOn)
                        {
                            audioSourcesHolder[key].pauseOn = false;
                            audioSource.UnPause();
                        } else {
                            audioSourcesHolder[key].pauseOn = true;
                            audioSource.Pause();
                        }
                    }

                    string mute = audioSourcesHolder[key].muteOn ? "Unmute" : "Mute";

                    if (GUILayout.Button(mute, GUILayout.Width(100)))
                    {
                        audioSourcesHolder[key].muteOn = !audioSourcesHolder[key].muteOn;
                        audioSource.mute = audioSourcesHolder[key].muteOn;
                    }

                    if (GUILayout.Button("Set Volume", GUILayout.Width(100)))
                    {
                        audioSource.volume = audioSourcesHolder[key].audioVolume;
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public static void toggleButton(string var, bool set, Action action)
        {
            if (GUILayout.Button(var + ": " + (set ? "ON" : "OFF")))
            {
                action.Invoke();
            }
        }

        public static float slider(string var, float x, float min, float max)
        {
            GUILayout.Space(5);
            GUILayout.Label($"{var}: {x}");
            x = (int)GUILayout.HorizontalSlider((float)x, (float)min, (float)max);
            GUILayout.Space(5);
            return x;
        }

        public static void actionButton(string var, Action action)
        {
            if (GUILayout.Button(var))
            {
                action.Invoke();
            }
        }
        private void Update()
        {
            if (Input.GetKeyUp(keyCodeOpenMenu))
            {
                showMenu = !showMenu;
            }

            if (Input.GetKeyUp(skipGameOverPlayKey))
            {
                skipGameOverPlay = !skipGameOverPlay;
                configSkipGameOverPlay.Value = skipGameOverPlay;

                if (skipGameOverPlay)
                {
                    Logger.LogInfo("skipGameOverPlay: on");
                    if (restartGameSkipGameOverPlay)
                    {
                        Logger.LogInfo("You must restart the game to use skipGameOverPlay");
                    }
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
                    if (restartGameResetCorruptionLevelAfterFuck)
                    {
                        Logger.LogInfo("You must restart the game to use resetCorruptionLevelAfterFuck");
                    }
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

        private static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
        private static Dictionary<string, AudioSourceHolder> audioSourcesHolder = new Dictionary<string, AudioSourceHolder>();


        public static void StartAndroidGalleryButton(AndroidGalleryButtonHandler __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("AndroidGalleryButton", aud);
            }
            catch { }
        }

        public static void StartAngelBehavior(AngelBehavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("Angel", aud);
            }
            catch { }
        }

        public static void StartBossFaeBehavior(BossFaeBehavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("BossFae", aud);
            }
            catch { }
        }

        public static void StartDeeBehaviour(DeeBehaviour __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("rumble").GetValue<AudioSource>();
                audioSources.Add("Dee", aud);
            }
            catch { }
        }

        public static void StartEggyBehaviour(eggyBehaviour __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("SFX").GetValue<AudioSource>();
                audioSources.Add("Eggy", aud);
            }
            catch { }
        }

        public static void StartEnemySFX(EnemySFX __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("m_audioSource").GetValue<AudioSource>();
                audioSources.Add("Enemy", aud);
            }
            catch { }
        }
        public static void StartFifi(FifiBehavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("HitSound").GetValue<AudioSource>();
                audioSources.Add("Fifi", aud);
            }
            catch { }
        }
        public static void StartGalleryButton(GalleryButtonHandler __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("GalleryButton", aud);
            }
            catch { }
        }

        public static void StartLeslie(leslie_behavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("Leslie", aud);
            }
            catch { }
        }

        public static void StartMallary(MallaryBehavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("CastSound").GetValue<AudioSource>();
                audioSources.Add("MallaryCastSound", aud);
                audioSources.Add("Mallary", __instance.gameObject.GetComponent<AudioSource>());
            }
            catch { }
        }
        public static void StartMax(MaxSFX __instance)
        {
            try
            {
                audioSources.Add("MaxVoice", __instance.Voice);
                audioSources.Add("MaxSFX", __instance.SFX);
                audioSources.Add("MaxSFXB", __instance.SFXB);
            }
            catch { }
        }

        public static void StartSlime(slimetower_behavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("SlimeTower", aud);
            }
            catch { }
        }
        public static void StartSphinx(SphinxRiddle __instance)
        {
            try
            {
                audioSources.Add("SphinxRiddle", __instance.AS);
            }
            catch { }
        }
        public static void StartSuccu(SuccuvickBehavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("aud").GetValue<AudioSource>();
                audioSources.Add("Succuvick", aud);
            }
            catch { }
        }

        public static void StartTifa(TifaBehavior __instance)
        {
            try
            {
                AudioSource aud = Traverse.Create(__instance).Field("TifaSFX").GetValue<AudioSource>();
                audioSources.Add("Tifa", aud);
            }
            catch { }
        }

        public static void StartVine(VineBehaviour __instance)
        {
            try
            {
                audioSources.Add("Vine", __instance.rumble);
            }
            catch { }
        }
        public static void StartLv3IntroScene(Lv3IntroScene __instance)
        {
            try
            {
                audioSources.Add("Lv3IntroScene", __instance.AS);
            }
            catch { }
        }
        public static void StartLv4BossIntroCallbacks(Lv4BossIntroCallbacks __instance)
        {
            try
            {
                AudioSource component = __instance.GetComponent<AudioSource>();
                audioSources.Add("Lv4BossIntroScene", component);
            }
            catch { }
        }

        public static void StartLv4BossIntroScene(Lv4BossIntroScene __instance)
        {
            try
            {
                audioSources.Add("Lv4BossIntroScene", __instance.AS);
            }
            catch { }
        }
        public static void StartLv4BossOutroCallbacks(Lv4BossOutroCallbacks __instance)
        {
            try
            {
                AudioSource component = __instance.GetComponent<AudioSource>();
                audioSources.Add("Lv4BossOutroCallbacks", component);
            }
            catch { }
        }
        public static void StartLv4BossOutroScene(Lv4BossOutroScene __instance)
        {
            try
            {
                audioSources.Add("Lv4BossOutroScene", __instance.AS);
            }
            catch { }
        }

        public static void StartLv4IntroScene(Lv4IntroScene __instance)
        {
            try
            {
                audioSources.Add("Lv4IntroScene", __instance.AS);
            }
            catch { }
        }
        public static void StartLv5BossIntroCallbacks(Lv5BossIntroCallbacks __instance)
        {
            try
            {
                AudioSource component = __instance.GetComponent<AudioSource>();
                audioSources.Add("Lv5BossIntroCallbacks", component);
            }
            catch { }
        }
        public static void StartLv5BossIntroScene(Lv5BossIntroScene __instance)
        {
            try
            {
                audioSources.Add("Lv5BossIntroScene", __instance.AS);
            }
            catch { }
        }
        public static void StartLv5BossOutroCallbacks(Lv5BossOutroCallbacks __instance)
        {
            try
            {
                AudioSource component = __instance.GetComponent<AudioSource>();
                audioSources.Add("Lv5BossOutroCallbacks", component);
            }
            catch { }
        }
        public static void StartLv5BossOutroScene(Lv5BossOutroScene __instance)
        {
            try
            {
                audioSources.Add("Lv5BossOutroScene", __instance.AS);
            }
            catch { }
        }
        public static void bossAudioBatches()
        {
            try
            {
                PatchHarmonyMethodUnity(typeof(Lv3IntroScene), "Start", "StartLv3IntroScene", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv4BossIntroCallbacks), "Start", "StartLv4BossIntroCallbacks", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv4BossIntroScene), "Start", "StartLv4BossIntroScene", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv4BossOutroCallbacks), "Start", "StartLv4BossOutroCallbacks", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv4BossOutroScene), "Start", "StartLv4BossOutroScene", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv4IntroScene), "Start", "StartLv4IntroScene", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv5BossIntroCallbacks), "Start", "StartLv5BossIntroCallbacks", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv5BossIntroScene), "Start", "StartLv5BossIntroScene", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv5BossOutroCallbacks), "Start", "StartLv5BossOutroCallbacks", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(Lv5BossOutroScene), "Start", "StartLv5BossOutroScene", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }
        public static void audioPatches()
        {
            bossAudioBatches();
            try
            {
                PatchHarmonyMethodUnity(typeof(AndroidGalleryButtonHandler), "Start", "StartAndroidGalleryButton", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(AngelBehavior), "Start", "StartAngelBehavior", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(BossFaeBehavior), "Start", "StartBossFaeBehavior", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(DeeBehaviour), "Start", "StartDeeBehaviour", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(eggyBehaviour), "Start", "StartEggyBehaviour", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(EnemySFX), "Start", "StartEnemySFX", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(FifiBehavior), "Start", "StartFifi", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(GalleryButtonHandler), "Start", "StartGalleryButton", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(leslie_behavior), "Start", "StartLeslie", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(MallaryBehavior), "Start", "StartMallary", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(MaxSFX), "Start", "StartMax", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(slimetower_behavior), "Start", "StartSlime", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(SphinxRiddle), "Start", "StartSphinx", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(SuccuvickBehavior), "Start", "StartSuccu", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(TifaBehavior), "Start", "StartTifa", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            try
            {
                PatchHarmonyMethodUnity(typeof(VineBehaviour), "Start", "StartVine", false, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }

        private static bool restartGameSkipGameOverPlay = false;
        private static bool restartGameResetCorruptionLevelAfterFuck = false;
        public static void PatchAllHarmonyMethods()
        {
            if (!enableMe)
            {
                return;
            }

            bool skipGameOverPlayTemp = skipGameOverPlay;
            bool resetCorruptionLevelAfterFuckTemp = resetCorruptionLevelAfterFuck;

            skipGameOverPlay = true;
            resetCorruptionLevelAfterFuck = true;

            audioPatches();

            if (!skipGameOverPlay)
            {
                restartGameSkipGameOverPlay = true;
            }

            if (!resetCorruptionLevelAfterFuck)
            {
                restartGameResetCorruptionLevelAfterFuck = true;
            }

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

            skipGameOverPlay = skipGameOverPlayTemp;
            resetCorruptionLevelAfterFuck = resetCorruptionLevelAfterFuckTemp;
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
