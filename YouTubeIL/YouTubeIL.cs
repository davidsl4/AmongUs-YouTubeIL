extern alias jb;
using System;
using System.Linq;
using System.Net;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace YouTubeIL
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [jb::JetBrains.Annotations.UsedImplicitly]
    // ReSharper disable once InconsistentNaming
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

        private void AddCustomServers()
        {
            const string name = "YouTube IL";
            // ReSharper disable StringLiteralTypo
            var ip = "youtubeil.amongus.ainedevs.com";
            // ReSharper restore StringLiteralTypo
            if (Uri.CheckHostName(ip) == UriHostNameType.Dns)
            {
                Log.LogMessage("Server address is a hostname, resolving " + ip + "...");
                try {
                    foreach (var address in Dns.GetHostAddresses(ip))
                    {
                        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                        ip = address.ToString();
                        Log.LogMessage("Hostname resolved to " + ip);
                        break;
                    }
                } catch {
                    Log.LogMessage("Hostname could not be resolved");
                }
            }
            
            const ushort port = 22023;
            
            var defaultRegions = ServerManager.DefaultRegions.ToList();
            Il2CppReferenceArray<ServerInfo> serverInfo = new ServerInfo[] { 
                new(name, ip, port)
            };
            
            defaultRegions.Insert(0,
                new StaticRegionInfo(name, StringNames.NoTranslation, null, serverInfo).Cast<IRegionInfo>());

            ServerManager.DefaultRegions = defaultRegions.ToArray();
        }
    }
}