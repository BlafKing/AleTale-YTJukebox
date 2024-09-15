using BepInEx;
using System.IO;
using UnityEngine;
using HarmonyLib;
using FMODUnity;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;
using Unity.Netcode;
using System;
using System.Runtime.CompilerServices;

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

        [ServerRpc(RequireOwnership = false)]
        public void SendMessageToAllServerRpc(string message, ServerRpcParams serverRpcParams = default) {
            SendMessageToAllClientsRpc(message);
        }

        [ClientRpc]
        private void SendMessageToAllClientsRpc(string message, ClientRpcParams clientRpcParams = default) {
            Debug.Log("Message received by all players: " + message);
        }
    }

    [BepInPlugin("com.tomdom.ytjukebox", "YTJukebox", "1.0.0")]
    public class Plugin : BaseUnityPlugin {
        private static Plugin instance;
        private static GameObject ytRPCPrefab;
        private static NetworkObject ytNetworkObject;
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

        public void CreatePrefab() {
            GameObject SteamNetManager = GameObject.Find("SteamNetManager");
            NetworkManager networkManager = SteamNetManager.GetComponent<NetworkManager>();

            ytRPCPrefab = new GameObject("YT-RPC");
            ytRPCPrefab.AddComponent<YtRPC>();
            ytNetworkObject = ytRPCPrefab.AddComponent<NetworkObject>();
            Debug.Log("created new ytRPCPrefab GameObject with components");
            networkManager.AddNetworkPrefab(ytRPCPrefab);
            Debug.Log("Added ytRPCPrefab to NetworkPrefabs");
            DontDestroyOnLoad(ytRPCPrefab);        }

        public void OnWorldLoad() {
            Audio.OnWorldLoad();
            AddEmptyTrack();
            UI.CreateCustomUI();

            GameObject SteamNetManager = GameObject.Find("SteamNetManager");
            NetworkManager networkManager = SteamNetManager.GetComponent<NetworkManager>();

            if (networkManager.IsHost) {
                ytNetworkObject.Spawn();
            }
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.P)) {
                GameObject YtRPCManager = GameObject.Find("YT-RPC");
                YtRPC ytRPCInstance = YtRPCManager.GetComponent<YtRPC>();
                ytRPCInstance.SendMessageToAllServerRpc("plasje van basje");
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
            FMOD.GUID staticGuid = new FMOD.GUID { Data1 = -502618873, Data2 = 1190340663, Data3 = 1799067022, Data4 = -1538532083 };
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