using NAudio.Wave;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;
using Unity.Netcode;

namespace YTJukeboxMod
{
    static internal class Audio
    {

        static private IWavePlayer waveOut;
        static private MediaFoundationReader mediaReader;
        static private StereoVolumeSampleProvider stereoProvider;
        static public GameObject activeJukebox;
        static public List<GameObject> jukeboxList;
        static private GameObject localPlayer;
        private static readonly float maxDistance = 80f;

        static public void OnWorldLoad()
        {
            if (waveOut == null)
            {
                var waveOutEvent = new WaveOutEvent
                {
                    DesiredLatency = 50,
                    NumberOfBuffers = 8
                };
                waveOut = waveOutEvent;
            }
            PlayerNet[] players = Object.FindObjectsOfType<PlayerNet>();
            foreach (PlayerNet playerNet in players)
            {
                NetworkBehaviour networkBehaviour = playerNet.GetComponent<NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.IsOwner)
                {
                    localPlayer = playerNet.gameObject;
                }
            }

            jukeboxList = new List<GameObject>();
        }

        static public void SetActiveJukebox(Jukebox JukeboxComponent)
        {
            activeJukebox = JukeboxComponent.gameObject;
        }

        static public void StopCustomTrack()
        {
            if (waveOut != null && mediaReader != null)
            {
                waveOut.Stop();
                mediaReader.Dispose();
                waveOut.Dispose();
                Log.Info("Track stopped.");
            }
        }

        static public void PlayCustomTrack(GameObject inputJukebox)
        {
            Jukebox JukeboxComponent = inputJukebox.GetComponent<Jukebox>();
            jukeboxList.Clear();
            jukeboxList.Add(inputJukebox);

            ChangeVolume(JukeboxComponent.volume.Value);
            JukeboxComponent.PlayerPlayServerRpc(99);

            if (File.Exists(ModPaths.customSong))
            {
                StopCustomTrack();

                mediaReader = new MediaFoundationReader(ModPaths.customSong);
                stereoProvider = new StereoVolumeSampleProvider(mediaReader.ToSampleProvider())
                {
                    LeftVolume = 1.0f,
                    RightVolume = 1.0f
                };

                waveOut.Init(stereoProvider);
                waveOut.Play();

            }
        }

        static public void ChangeVolume(byte Volume)
        {
            if (waveOut != null)
            {
                float volumeLevel = Mathf.Clamp01(Volume / 100f);
                waveOut.Volume = volumeLevel;
            }
        }

        static public void UpdateAudioSpatial()
        {
            if (localPlayer != null && jukeboxList.Count > 0)
            {
                Vector3 playerPos = localPlayer.transform.position;

                float totalLeftVolume = 0f;
                float totalRightVolume = 0f;

                foreach (GameObject jukebox in jukeboxList)
                {
                    if (jukebox != null)
                    {
                        Vector3 jukeboxPos = jukebox.transform.position;
                        Jukebox jukeboxComp = jukebox.GetComponent<Jukebox>();

                        float distance = Mathf.Max(2.0f, Vector3.Distance(playerPos, jukeboxPos));
                        float distanceRatio = Mathf.Clamp01((distance - 2.0f) / (maxDistance - 2.0f));
                        float volume = Mathf.Pow(1 - distanceRatio, 10f) * (jukeboxComp.volume.Value / 100f);

                        Vector3 directionToPlayer = (playerPos - jukeboxPos).normalized;
                        float pan = Vector3.Dot(directionToPlayer, localPlayer.transform.right);

                        float leftVolume = volume * Mathf.Clamp01(1f - pan);
                        float rightVolume = volume * Mathf.Clamp01(1f + pan);

                        totalLeftVolume = Mathf.Clamp01(totalLeftVolume + leftVolume);
                        totalRightVolume = Mathf.Clamp01(totalRightVolume + rightVolume);
                    }
                }

                stereoProvider.LeftVolume = totalLeftVolume;
                stereoProvider.RightVolume = totalRightVolume;
            }
        }

        static public List<GameObject> GetAllJukeboxes()
        {
            GameObject jukeboxManagerObject = GameObject.Find("Common/Game");
            JukeboxManager jukeboxManager = jukeboxManagerObject.GetComponent<JukeboxManager>();

            Type jukeboxManagerType = typeof(JukeboxManager);
            FieldInfo fieldInfo = jukeboxManagerType.GetField("_jukeboxes", BindingFlags.NonPublic | BindingFlags.Instance);

            Dictionary<ushort, Jukebox> jukeboxesDict = fieldInfo.GetValue(jukeboxManager) as Dictionary<ushort, Jukebox>;
            List<GameObject> jukeboxGameObjects = new List<GameObject>();
            foreach (var jukeboxEntry in jukeboxesDict)
            {
                Jukebox jukeboxComponent = jukeboxEntry.Value;
                if (jukeboxComponent != null)
                {
                    GameObject jukeboxGameObject = jukeboxComponent.gameObject;
                    jukeboxGameObjects.Add(jukeboxGameObject);
                }
            }
            return jukeboxGameObjects;
        }
    }

    public class StereoVolumeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private float leftVolume = 1.0f;
        private float rightVolume = 1.0f;

        public StereoVolumeSampleProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source sample provider must be stereo");
            }
            this.source = source;
        }

        public float LeftVolume
        {
            get { return leftVolume; }
            set { leftVolume = Math.Max(0, Math.Min(1, value)); }
        }

        public float RightVolume
        {
            get { return rightVolume; }
            set { rightVolume = Math.Max(0, Math.Min(1, value)); }
        }

        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);
            for (int i = 0; i < samplesRead; i += 2)
            {
                buffer[offset + i + 1] *= leftVolume;
                buffer[offset + i] *= rightVolume;
            }
            return samplesRead;
        }
    }

}