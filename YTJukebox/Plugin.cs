using BepInEx;
using FMODUnity;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using YTJukebox;

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
            root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            customSong = Path.Combine(root, "AudioTrack.wav");
            dependencies = Path.Combine(root, "Dependencies");
            ffmpeg = Path.Combine(dependencies, "ffmpeg.exe");
            yt_dlp = Path.Combine(dependencies, "yt-dlp.exe");
        }
    }

    [BepInPlugin("com.tomdom.ytjukebox", "YTJukebox", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        public GameObject ytRpcPrefab;
        public NetworkManager networkManager;

        private void Awake()
        {
            ModPaths.SetPaths();

            if (!Directory.Exists(ModPaths.dependencies))
            {
                List<string> filesToMove = new List<string>
                {
                    "NAudio.Core.dll",
                    "NAudio.Wasapi.dll",
                    "NAudio.WinMM.dll",
                    "NAudio.dll",
                    "ytbIcon.png",
                    "ytnetcode"
                };
                Directory.CreateDirectory(ModPaths.dependencies);

                foreach (string fileName in filesToMove)
                {
                    string sourcePath = Path.Combine(ModPaths.root, fileName);
                    string destinationPath = Path.Combine(ModPaths.dependencies, fileName);
                    if (File.Exists(sourcePath))
                    {
                        File.Move(sourcePath, destinationPath);
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

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

            string assetDir = Path.Combine(ModPaths.dependencies, "ytnetcode");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);
            ytRpcPrefab = bundle.LoadAsset<GameObject>("Assets/YTJukebox/YTNetworkManager.prefab");
            ytRpcPrefab.AddComponent<YTNetworkManager>();

            if (!File.Exists(ModPaths.yt_dlp) || !File.Exists(ModPaths.ffmpeg))
            {
                Log.Info("yt-dlp or ffmpeg not found! triggering download");
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
        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";

            string[] searchPaths = new string[]
            {
            Path.Combine(ModPaths.root, assemblyName),
            Path.Combine(ModPaths.dependencies, assemblyName)
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }
            }

            return null;
        }
        public void LogInfo(string message) {Logger.LogInfo(message);}
        public void LogError(string message) {Logger.LogError(message);}
        public void LogWarning(string message) {Logger.LogWarning(message);}
    }
    static public class Log
    {
        static public void Info(string message) {Plugin.instance.LogInfo(message);}
        static public void Error(string message) {Plugin.instance.LogError(message);}
        static public void Warning(string message) {Plugin.instance.LogWarning(message);}
    }
}