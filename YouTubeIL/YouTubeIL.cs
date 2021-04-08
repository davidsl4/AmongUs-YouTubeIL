extern alias jb;
using System.Linq;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace YouTubeIL
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [jb::JetBrains.Annotations.UsedImplicitly]
    public class YouTubeIL : BasePlugin
    {
        [jb::JetBrains.Annotations.UsedImplicitly]
        public const string Id = "il.r0den.YouTubeIL";

        internal static ManualLogSource Logger;
        private Harmony Harmony { get; } = new(Id);
        
        public override void Load()
        {
            Logger = Log;
            Logger.LogInfo("YouTubeIL plugin loading...");
            Harmony.PatchAll();
            
            Patches.VersionShower.Initialize();
            AddCustomServers();
        }

        private static void AddCustomServers()
        {
            const string name = "YouTube IL";
            // ReSharper disable StringLiteralTypo
            const string hostname = "youtubeil.amongus.ainedevs.com";
            // ReSharper restore StringLiteralTypo
            const ushort port = 22023;
            
            var defaultRegions = ServerManager.DefaultRegions.ToList();
            defaultRegions.Insert(0,
                new DnsRegionInfo(hostname, name, StringNames.NoTranslation, "127.0.0.1", port).Cast<IRegionInfo>());
            ServerManager.DefaultRegions = defaultRegions.ToArray();
        }
    }
}