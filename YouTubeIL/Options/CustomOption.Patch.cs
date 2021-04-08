extern alias jb;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using jb::JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YouTubeIL.Options
{
    [HarmonyPatch]
    internal abstract partial class CustomOption
    {
        private static int _defaultGameOptionsCount;
        
        private static IEnumerable<OptionBehaviour> GetGameOptions(float lowestY)
        {
            var options = new List<OptionBehaviour>();

            var toggleOption = Object.FindObjectsOfType<ToggleOption>().FirstOrDefault();
            var numberOption = Object.FindObjectsOfType<NumberOption>().FirstOrDefault();
            var stringOption = Object.FindObjectsOfType<StringOption>().FirstOrDefault();

            var i = 0;
            foreach (var option in Options)
            {
                if (option.GameOption != null)
                {
                    option.GameOption.gameObject.SetActive(true);

                    options.Add(option.GameOption);

                    continue;
                }

                switch (option.OptionType)
                {
                    case CustomOptionType.Toggle when AmongUsClient.Instance is {AmHost: true}:
                    {
                        if (toggleOption == null) continue;

                        var toggle = Object.Instantiate(toggleOption, toggleOption.transform.parent);

                        if (!option.GameOptionCreated(toggle))
                        {
                            Object.Destroy(toggle);

                            continue;
                        }

                        options.Add(toggle);
                        break;
                    }
                    case CustomOptionType.Number:
                    {
                        if (numberOption == null)
                        {
                            YouTubeIL.Logger.LogWarning("No default number option found to become prefab for: " +
                                                        option.ID);
                            continue;
                        }
                        
                        var number = Object.Instantiate(numberOption, numberOption.transform.parent);

                        if (!option.GameOptionCreated(number))
                        {
                            Object.Destroy(number);

                            continue;
                        }

                        options.Add(number);
                        break;
                    }
                    case CustomOptionType.String:
                    case CustomOptionType.Toggle when AmongUsClient.Instance is {AmHost: false}:
                    {
                        if (stringOption == null)
                        {
                            YouTubeIL.Logger.LogWarning("No default string option found to become prefab for: " +
                                                        option.ID);
                            continue;
                        }

                        var str = Object.Instantiate(stringOption, stringOption.transform.parent);

                        if (!option.GameOptionCreated(str))
                        {
                            Object.Destroy(str);

                            continue;
                        }

                        options.Add(str);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!option.GameOption) continue;

                var transform = option.GameOption.transform;
                var localPosition = transform.localPosition;
                localPosition = new Vector3(localPosition.x,
                    lowestY - ++i * 0.5F, localPosition.z);
                transform.localPosition = localPosition;

                option.GameOption.gameObject.SetActive(true);
            }

            return options;
        }
        
        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void GameOptionsMenuStart(GameOptionsMenu __instance)
        {
            var customOptions = GetGameOptions(__instance.GetComponentsInChildren<OptionBehaviour>()
                .Min(option => option.transform.localPosition.y));
            OptionBehaviour[] defaultOptions = __instance.Children;

            _defaultGameOptionsCount = defaultOptions.Length;

            __instance.Children = defaultOptions.Concat(customOptions).ToArray();
        }
        
        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
        [HarmonyPostfix]
        [SuppressMessage("ReSharper", "Unity.NoNullPropagation")]
        // ReSharper disable once InconsistentNaming
        private static void GameOptionsMenuUpdate(GameOptionsMenu __instance)
        {
            if (Options.Count > 0)
            {
                var options = __instance.Children.Take(_defaultGameOptionsCount).ToList();

                var lowestY = options.Min(option => option.transform.localPosition.y);
                var i = 0;

                foreach (var option in Options.Where(option => option.GameOption?.gameObject != null))
                {
                    option.GameOption.gameObject.SetActive(true);
                    option.GameOption.enabled = option.ParentOption == null || option.ParentOption.IsValueEnabled();

                    var transform = option.GameOption.transform;
                    var localPosition = transform.localPosition;
                    localPosition = new Vector3(localPosition.x,
                        lowestY - ++i * 0.5F, localPosition.z);
                    transform.localPosition = localPosition;

                    options.Add(option.GameOption);
                }

                __instance.Children = options.ToArray();
            }

            __instance.GetComponentInParent<Scroller>().YBounds.max = (__instance.Children.Length - 7) * 0.5F + 0.13F;
        }
        
        [HarmonyPatch]
        private static class GameOptionsDataPatch
        {
            [UsedImplicitly]
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var type = typeof(GameOptionsData);
                return type.GetMethods(AccessTools.all).Where(x =>
                    x.ReturnType == typeof(string) &&
                    x.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(int) }));
                //return typeof(GameOptionsData).GetMethods(typeof(string), typeof(int));
            }

            [UsedImplicitly]
            // ReSharper disable once InconsistentNaming
            private static void Postfix(ref string __result)
            {
                __result = Options.Where(o => o.ParentOption == null || o.ParentOption.IsValueEnabled())
                    .Aggregate(__result, (current, customOption) => current + $"{customOption}\n");
                if (HudManager.Instance.GameSettings != null)
                    HudManager.Instance.GameSettings.scale = 0.5f;
            }
        }

        [HarmonyPatch]
        private static class OnEnablePatch
        {
            [UsedImplicitly]
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new (Type type, string name)[]
                {
                    (typeof(ToggleOption), nameof(ToggleOption.OnEnable)),
                    (typeof(NumberOption), nameof(NumberOption.OnEnable)),
                    (typeof(StringOption), nameof(StringOption.OnEnable))
                };
                foreach (var (type, methodName) in methods)
                {
                    yield return type.GetMethod(methodName);
                }
            }
            
            private static bool OnEnable(OptionBehaviour opt)
            {
                var customOption = Options.FirstOrDefault(option => option.GameOption == opt);

                if (customOption == null) return true;

                customOption.GameOptionCreated(opt);

                return false;
            }
            
            [UsedImplicitly]
            // ReSharper disable once InconsistentNaming
            private static bool Prefix(OptionBehaviour __instance)
            {
                return OnEnable(__instance);
            }
        }

        [HarmonyPatch]
        private static class FixedUpdatePatch
        {
            [UsedImplicitly]
            private static IEnumerable<MethodBase> TargetMethods()
            {
                var methods = new (Type type, string name)[]
                {
                    (typeof(ToggleOption), nameof(ToggleOption.FixedUpdate)),
                    (typeof(NumberOption), nameof(NumberOption.FixedUpdate)),
                    (typeof(StringOption), nameof(StringOption.FixedUpdate))
                };
                foreach (var (type, methodName) in methods)
                {
                    yield return type.GetMethod(methodName);
                }
            }
            
            [UsedImplicitly]
            // ReSharper disable once InconsistentNaming
            private static bool Prefix(OptionBehaviour __instance)
            {
                return Options.All(o => o.GameOption != __instance);
            }
        }

        [HarmonyPatch(typeof(ToggleOption), nameof(ToggleOption.Toggle))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool ToggleButtonPatch(ToggleOption __instance)
        {
            var option = Options.FirstOrDefault(o => o.GameOption == __instance);

            if (option is not IToggleOption toggle) return true;
            
            toggle.Toggle();
            return false;
        }
        
        [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool NumberOptionPatchIncrease(NumberOption __instance)
        {
            var option = Options.FirstOrDefault(o => o.GameOption == __instance);

            if (option is not INumberOption number) return true;
            
            number.Increase();
            return false;
        }
        
        [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool NumberOptionPatchDecrease(NumberOption __instance)
        {
            var option = Options.FirstOrDefault(o => o.GameOption == __instance);

            if (option is not INumberOption number) return true;
            
            number.Decrease();
            return false;
        }
        
        [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool StringOptionPatchIncrease(StringOption __instance)
        {
            var option = Options.FirstOrDefault(o => o.GameOption == __instance);

            if (option is not IStringOption str) return true;
            
            str.Increase();
            return false;
        }
        
        [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool StringOptionPatchDecrease(StringOption __instance)
        {
            var option = Options.FirstOrDefault(o => o.GameOption == __instance);

            if (option is not IStringOption str) return true;
            
            str.Decrease();
            return false;
        }
        
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
        [HarmonyPostfix]
        private static void PlayerControlPatch()
        {
            if (ReferenceEquals(AmongUsClient.Instance, null) || AmongUsClient.Instance.AmHost != true ||
                PlayerControl.AllPlayerControls.Count < 2 || !PlayerControl.LocalPlayer) return;

            
            SendSyncCustomOptions(Options.ToArray());
        }
    }
}