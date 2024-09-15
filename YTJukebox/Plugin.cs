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
        private int playersDownloaded = 0;

        // Called by either the host or a client
        [ServerRpc(RequireOwnership = false)]
        public void TriggerDownloadServerRpc(string inputURL, ServerRpcParams serverRpcParams = default) {
            Debug.Log("TriggerDownloadServerRpc called on the server or host");

            // This will run on the host. It sends the request to all clients.
            BroadcastDownloadClientRpc(inputURL);
        }

        // Broadcasts the download event to all clients, including the host
        [ClientRpc]
        private void BroadcastDownloadClientRpc(string inputURL, ClientRpcParams clientRpcParams = default) {
            Debug.Log($"BroadcastDownloadClientRpc received on client for URL: {inputURL}");

            // Start the download on each client
            StartDownloadFileAsync(inputURL);
        }

        // Client-side asynchronous download logic
        private async void StartDownloadFileAsync(string inputURL) {
            Debug.Log($"StartDownloadFileAsync started on client for URL: {inputURL}");

            // Perform the download operation (assumes Download.GetCustomSong is async)
            await Download.GetCustomSong(inputURL);

            // Once download is done, notify the server (host)
            NotifyDownloadCompleteServerRpc();
        }

        // Notify the host when a client has completed the download
        [ServerRpc(RequireOwnership = false)]
        private void NotifyDownloadCompleteServerRpc(ServerRpcParams serverRpcParams = default) {
            Debug.Log("NotifyDownloadCompleteServerRpc called by a client");

            playersDownloaded++;

            // Check if all players have completed the download
            if (playersDownloaded == PlayerManager.Instance.players.Count) {
                Debug.Log("All players have completed the download.");
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