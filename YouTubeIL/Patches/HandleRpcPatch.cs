extern alias jb;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using jb::JetBrains.Annotations;
using YouTubeIL.Networking;

namespace YouTubeIL.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class HandleRpcPatch
    {
        private const byte CallId = byte.MaxValue - 1; // leave byte.MaxValue to Reactor

        internal delegate void CustomRpcHandler(MessageReader messageReader);
        private static readonly Dictionary<CustomRpc, CustomRpcHandler> CustomRpcHandlers = new();
        
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId,
            [HarmonyArgument(1)] MessageReader reader)
        {
            if (callId != CallId) return true;
            
            // Validity check
            if (reader.ReadString() != YouTubeIL.Id)
                return true;

            var rpcId = (CustomRpc)reader.ReadByte();

            switch (rpcId)
            {
                case CustomRpc.SyncCustomOption:
                    break;
                default:
                    YouTubeIL.Logger.LogError($"Invalid RPC packet received ({(byte)rpcId}).");
                    return true;
            }
            

            return true;
        }

        internal static bool RegisterCustomRpc(CustomRpc rpc, CustomRpcHandler handler) =>
            CustomRpcHandlers.TryAdd(rpc, handler);

        internal static bool UnregisterCustomRpc(CustomRpc rpc) => CustomRpcHandlers.Remove(rpc);

    }
}