using HarmonyLib;
using Unity.Netcode;

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
                Audio.StopCustomTrack();
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerPlayServerRpc", MethodType.Normal)]
        private class StopPatch
        {
            static void Postfix()
            {
                Audio.StopCustomTrack();
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerStopServerRpc", MethodType.Normal)]
        private class PlayPatch
        {
            static void Postfix()
            {
                Audio.StopCustomTrack();
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerNextServerRpc", MethodType.Normal)]
        private class NextPatch
        {
            static void Postfix()
            {
                Audio.StopCustomTrack();
            }
        }

        [HarmonyPatch(typeof(Jukebox), "PlayerPreviousServerRpc", MethodType.Normal)]
        private class PrevPatch
        {
            static void Postfix()
            {
                Audio.StopCustomTrack();
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
