using BepInEx;
using System.IO;
using UnityEngine;
using HarmonyLib;
using FMODUnity;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using Unity.Netcode;

namespace YTJukeboxMod {
    static public class ModPaths {
        static public string root;
        static public string customSong;
        static public string dependencies;
        static public string ffmpeg;
        static public string yt_dlp;

        static public void SetPaths() {
            root = Path.Combine(Paths.PluginPath, "YTJukebox");
            customSong = Path.Combine(root, "AudioTrack.wav");
            dependencies = Path.Combine(root, "Dependencies");
            ffmpeg = Path.Combine(dependencies, "ffmpeg.exe");
            yt_dlp = Path.Combine(dependencies, "yt-dlp.exe");
        }
    }

    public class YtRPC : NetworkBehaviour {
        // This function is called by a client or host to send a message to all clients
        [ServerRpc(RequireOwnership = false)]
        public void SendMessageServerRpc(string message, ServerRpcParams serverRpcParams = default) {
            // Only the server or host should handle broadcasting the message
            if (!IsServer && !IsHost) {
                return;
            }

            // Broadcast the message to all clients, including the host
            SendMessageToClientsClientRpc(message);
        }

        // This function will be called on all clients (including the host) to receive the message
        [ClientRpc]
        private void SendMessageToClientsClientRpc(string message, ClientRpcParams clientRpcParams = default) {
            Debug.Log($"Message received on client: {message}");
        }

        // This function can be used to trigger the message from either the host or client
        public void TriggerMessage(string message) {
            if (IsServer || IsHost) {
                // If it's the host or server, just send the message directly to all clients
                SendMessageToClientsClientRpc(message);
            }
            else {
                // If it's a client, send the message to the server to broadcast to everyone
                SendMessageServerRpc(message);
            }
        }
    }

    [BepInPlugin("com.tomdom.ytjukebox", "YTJukebox", "1.0.0")]
    public class Plugin : BaseUnityPlugin {
        private static Plugin instance;
        private static YtRPC ytRPCInstance;

        private void Awake() {
            instance = this;
            ModPaths.SetPaths();

            if (!File.Exists(ModPaths.yt_dlp) || !File.Exists(ModPaths.ffmpeg)) {
                Debug.Log("yt-dlp or ffmpeg not found! triggering download");
                Task.Run(async () => await Download.GetDependencies());
            }

            HarmonyPatches.Init();
            Harmony harmony = new Harmony("com.tomdom.ytjukebox");
            harmony.PatchAll();
        }

        public static Plugin GetInstance() {
            return instance;
        }

        public static YtRPC GetYtRpcInstance() {
            return ytRPCInstance;
        }

        public void OnWorldLoad() {
            Audio.OnWorldLoad();
            AddEmptyTrack();

            GameObject YtRPCManager = new GameObject("YTJukebox RPC Manager");
            GameObject Common = GameObject.Find("Common");
            YtRPCManager.transform.SetParent(Common.transform, false);
            ytRPCInstance = YtRPCManager.AddComponent<YtRPC>();
            UI.CreateCustomUI();
        }

        private void Update() {
            if (UI.GameCanvas) {
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    if (UI.Youtube.activeSelf == true) {
                        UI.Youtube.SetActive(false);
                        UI.Jukebox.SetActive(true);
                        UI.gameMenu.enabled = true;
                    }
                }
            }
        }

        private void FixedUpdate() {
            Audio.UpdateAudioSpatial();
        }

        private void AddEmptyTrack() {
            var staticGuid = new FMOD.GUID { Data1 = -502618873, Data2 = 1190340663, Data3 = 1799067022, Data4 = -1538532083 };

            if (!JukeboxManager.Instance.tracks.ContainsKey(99)) {
                JukeboxManager.Instance.tracks.Add(99, new JukeboxTrack {
                    id = 99,
                    eventRef = new EventReference { Guid = staticGuid },
                    name = "",
                    questId = 0
                });
            }
        }
    }
}