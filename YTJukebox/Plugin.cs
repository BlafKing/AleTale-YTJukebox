using BepInEx;
using FMODUnity;
using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using YTJukebox;
using Debug = UnityEngine.Debug;

namespace YTJukeboxMod
{
    static public class ModPaths
    {
        static public string root;
        static public string customSong;
        static public string dependencies;
        static public string ffmpeg;
        static public string yt_dlp;

        static public void SetPaths()
        {
            root = Path.Combine(Paths.PluginPath, "YTJukebox");
            customSong = Path.Combine(root, "AudioTrack.wav");
            dependencies = Path.Combine(root, "Dependencies");
            ffmpeg = Path.Combine(dependencies, "ffmpeg.exe");
            yt_dlp = Path.Combine(dependencies, "yt-dlp.exe");
        }
    }

    [BepInPlugin("com.tomdom.ytjukebox", "YTJukebox", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        public GameObject ytRpcPrefab;
        public NetworkManager networkManager;

        private void Awake()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            instance = this;
            ModPaths.SetPaths();

            string assetDir = Path.Combine(ModPaths.dependencies, "ytnetcode");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);
            ytRpcPrefab = bundle.LoadAsset<GameObject>("Assets/YTJukebox/YTNetworkManager.prefab");
            ytRpcPrefab.AddComponent<YTNetworkManager>();

            if (!File.Exists(ModPaths.yt_dlp) || !File.Exists(ModPaths.ffmpeg))
            {
                Debug.Log("yt-dlp or ffmpeg not found! triggering download");
                Task.Run(async () => await Download.GetDependencies());
            }

            HarmonyPatches.Init();
            Harmony harmony = new Harmony("com.tomdom.ytjukebox");
            harmony.PatchAll();
        }

        public void OnWorldLoad()
        {
            Audio.OnWorldLoad();
            AddEmptyTrack();
            UI.CreateCustomUI();

            GameObject SteamNetManager = GameObject.Find("SteamNetManager");
            networkManager = SteamNetManager.GetComponent<NetworkManager>();

            if (networkManager.IsHost)
            {
                GameObject YtRpc = Instantiate(ytRpcPrefab);
                YtRpc.GetComponent<NetworkObject>().Spawn();
            }
        }

        private void Update()
        {
            if (UI.GameCanvas)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (UI.Youtube.activeSelf == true)
                    {
                        UI.Youtube.SetActive(false);
                        UI.Jukebox.SetActive(true);
                        UI.gameMenu.enabled = true;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            Audio.UpdateAudioSpatial();
        }

        private void AddEmptyTrack()
        {
            FMOD.GUID staticGuid = new FMOD.GUID { Data1 = -502618873, Data2 = 1190340663, Data3 = 1799067022, Data4 = -1538532083 };
            if (!JukeboxManager.Instance.tracks.ContainsKey(99))
            {
                JukeboxManager.Instance.tracks.Add(99, new JukeboxTrack
                {
                    id = 99,
                    eventRef = new EventReference { Guid = staticGuid },
                    name = "",
                    questId = 0
                });
            }
        }
    }
}