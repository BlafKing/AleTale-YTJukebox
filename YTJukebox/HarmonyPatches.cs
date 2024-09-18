using HarmonyLib;
using Unity.Netcode;
using YTJukebox;

namespace YTJukeboxMod
{
    static internal class HarmonyPatches
    {

        static private Plugin main;

        static public void Init()
        {
            main = Plugin.instance;
        }

        [HarmonyPatch(typeof(SteamManager), "Start", MethodType.Normal)]
        private class NetManagerPatch
        {
            static void Postfix(ref SteamManager __instance)
            {
                __instance.GetComponent<NetworkManager>().AddNetworkPrefab(Plugin.instance.ytRpcPrefab);
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "Start", MethodType.Normal)]
        private class PlayerManagerStartPatch
        {
            static void Postfix()
            {
                main.OnWorldLoad();
            }
        }

        [HarmonyPatch(typeof(SteamManager), "Disconnect", MethodType.Normal)]
        private class LeaveWorldPatch
        {
            static void Postfix()
            {
                YTNetworkManager.instance.StopTrackServerRpc();
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerPlayServerRpc", MethodType.Normal)]
        private class PlayPatch
        {
            static void Postfix(byte id)
            {
                if (Audio.isPlaying && id != 99)
                {
                    YTNetworkManager.instance.StopTrackServerRpc();
                }
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerStopServerRpc", MethodType.Normal)]
        private class StopPatch
        {
            static void Postfix()
            {
                YTNetworkManager.instance.StopTrackServerRpc();
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerNextServerRpc", MethodType.Normal)]
        private class NextPatch
        {
            static void Postfix()
            {
                if (Audio.isPlaying)
                {
                    YTNetworkManager.instance.StopTrackServerRpc();
                }
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerPreviousServerRpc", MethodType.Normal)]
        private class PrevPatch
        {
            static void Postfix()
            {
                if (Audio.isPlaying)
                {
                    YTNetworkManager.instance.StopTrackServerRpc();
                }
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerVolumeServerRpc", MethodType.Normal)]
        private class VolumePatch
        {
            static void Postfix(byte v)
            {
                Audio.ChangeVolume(v);
            }
        }

        [HarmonyPatch(typeof(Jukebox), "Interact", MethodType.Normal)]
        private class InteractPatch
        {
            static void Postfix(Jukebox __instance, Interactive.Event e, ushort itemDataId, uint itemId)
            {
                if (e == Interactive.Event.UseDown)
                {
                    Audio.SetActiveJukebox(__instance);
                }
            }
        }
    }
}
