using System;
using System.Reflection;
using BepInEx.IL2CPP;
using Reactor;
using Reactor.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace YouTubeIL.Patches
{
    internal static class VersionShower
    {
        private static bool _reactorSubscribed;
        private static (Material material, TextAsset fontData)? _textRendererPrefab; 
        
        public static void Initialize()
        {
            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((_, _) =>
            {
                var reactorStateChanged = IL2CPPChainloader.Instance.Plugins.ContainsKey(ReactorPlugin.Id) !=
                                          _reactorSubscribed;
                
                // If Reactor was loaded previously and no longer available or otherwise it wasn't been loaded and now already
                if (reactorStateChanged)
                {
                    ((Action)(() => // Reactor used methods - be safe
                    {
                        if (!_reactorSubscribed) // Subscribe a method if Reactor plugin now loaded
                            ReactorVersionShower.TextUpdated += UpdateText;
                        // Unsubscribing not required because the plugin isn't initialized, and when it will become available
                        // the subscribers list will be empty
                    })).Invoke();

                    // Flip the flag
                    _reactorSubscribed = !_reactorSubscribed;
                }
                // If Reactor plugin state haven't changed and weren't subscribed yet, we have to add a prefab label and output there
                else if (!_reactorSubscribed)
                {
                    var original = Object.FindObjectOfType<global::VersionShower>();
                    if (!original)
                        return;

                    // Copied from Reactor.Extensions.UnityObjectExtensions
                    // Reason: If Reactor unloaded the methods will not work (since the dll not there)
                    static T DoNotDestroy<T>(T obj) where T : Object
                    {
                        obj.hideFlags |= HideFlags.HideAndDontSave;
                        Object.DontDestroyOnLoad(obj);
                        return obj;
                    }

                    var originalTextRenderer = original.gameObject.GetComponentInChildren<TextRenderer>();
                    _textRendererPrefab ??= (DoNotDestroy(Object.Instantiate(originalTextRenderer.GetComponent<MeshRenderer>().material)),
                        DoNotDestroy(Object.Instantiate(originalTextRenderer.FontData)));

                    var gameObject = new GameObject("YouTubeIL version " + Guid.NewGuid());
                    gameObject.transform.parent = original.transform.parent;

                    var aspectPosition = gameObject.AddComponent<AspectPosition>();
                    aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftTop;
                    var position = original.GetComponent<AspectPosition>().DistanceFromEdge;
                    position.y += 0.2f;
                    aspectPosition.DistanceFromEdge = position;
                    aspectPosition.AdjustPosition();

                    gameObject.AddComponent<MeshRenderer>().material = _textRendererPrefab.Value.material;
                    gameObject.AddComponent<MeshFilter>();
                    var textRenderer = gameObject.AddComponent<TextRenderer>();
                    textRenderer.FontData = _textRendererPrefab.Value.fontData;
                    textRenderer.scale = 0.65f;
                    UpdateText(textRenderer);
                }
            }));
        }

        private static string GetLabelText() => "YouTubeIL " + typeof(YouTubeIL).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        private static void UpdateText(TextRenderer text)
        {
            var labelText = GetLabelText();
            if (string.IsNullOrWhiteSpace(text.Text))
                text.Text = labelText;
            else
            {
                var index = text.Text.IndexOf('\n');
                text.Text = text.Text.Insert(index == -1 ? text.Text.Length - 1 : index, $"\n{labelText}");
            }
        }
    }
}