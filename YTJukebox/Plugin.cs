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

        [ServerRpc(RequireOwnership = false)]
        public void TriggerDownloadServerRpc(string inputURL, ServerRpcParams serverRpcParams = default) {
            Debug.Log("TriggerDownloadServerRpc");

            // Get the NetworkManager instance
            NetworkManager networkManager = NetworkManager.Singleton;

            // Ensure the network manager is valid and listening
            if (networkManager == null || !networkManager.IsListening) {
                Debug.Log("NetworkManager is null or not listening");
                return;
            }

            // Check if the current instance is the server or host
            if (!networkManager.IsServer && !networkManager.IsHost) {
                Debug.Log("User is not server nor host");
                return;
            }

            // Trigger download on all clients
            BroadcastDownloadClientRpc(inputURL);
        }

        [ClientRpc]
        private void BroadcastDownloadClientRpc(string inputURL, ClientRpcParams clientRpcParams = default) {
            Debug.Log("BroadcastDownloadClientRpc");
            StartDownloadFileAsync(inputURL);
        }

        private async void StartDownloadFileAsync(string inputURL) {
            Debug.Log($"StartDownloadFileAsync started on client for URL: {inputURL}");

            // Perform the download operation asynchronously
            await Download.GetCustomSong(inputURL);

            // Notify server when the download is complete
            NotifyDownloadCompleteServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyDownloadCompleteServerRpc(ServerRpcParams serverRpcParams = default) {
            Debug.Log("NotifyDownloadCompleteServerRpc");

            // Get the NetworkManager instance
            NetworkManager networkManager = NetworkManager.Singleton;

            // Ensure the network manager is valid and listening
            if (networkManager == null || !networkManager.IsListening) {
                Debug.Log("NetworkManager is null or not listening");
                return;
            }

            // Check if the current instance is the server or host
            if (!networkManager.IsServer && !networkManager.IsHost) {
                Debug.Log("User is not server nor host");
                return;
            }

            // Increment the number of players who have completed the download
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

            if (Input.GetKeyDown(KeyCode.P)) {
                Debug.Log(PlayerManager.Instance.players.Count);
            }

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