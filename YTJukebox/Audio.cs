using NAudio.Wave;
using System;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace YTJukeboxMod {
    static internal class Audio {

        static private IWavePlayer waveOut;
        static private MediaFoundationReader mediaReader;
        static private StereoVolumeSampleProvider stereoProvider;
        static private GameObject activeJukebox;
        static private Jukebox activeJukeboxComp;
        static private GameObject mainCamera;
        static public bool isPlaying = false;
        private static readonly float maxDistance = 80f;

        static public void OnWorldLoad() {
            if (waveOut == null) {
                var waveOutEvent = new WaveOutEvent {
                    DesiredLatency = 50,
                    NumberOfBuffers = 8
                };
                waveOut = waveOutEvent;
            }
            mainCamera = GameObject.Find("MainCamera");
        }

        static public void SetActiveJukebox(Jukebox JukeboxComponent) {
            activeJukebox = JukeboxComponent.gameObject;
            activeJukeboxComp = JukeboxComponent;
            ChangeVolume(activeJukeboxComp.volume.Value);
        }

        static public void StopCustomTrack() {
            if (waveOut != null && mediaReader != null && isPlaying) {
                waveOut.Stop();
                mediaReader.Dispose();
                waveOut.Dispose();
                isPlaying = false;
                Debug.Log("Track stopped.");
            }
        }

        static public void PlayCustomTrack() {
            activeJukeboxComp.PlayerPlayServerRpc(99);
            if (File.Exists(ModPaths.customSong)) {
                if (isPlaying) {
                    StopCustomTrack();
                }

                mediaReader = new MediaFoundationReader(ModPaths.customSong);
                stereoProvider = new StereoVolumeSampleProvider(mediaReader.ToSampleProvider()) {
                    LeftVolume = 1.0f,
                    RightVolume = 1.0f
                };

                waveOut.Init(stereoProvider);
                waveOut.Play();

                isPlaying = true;
            }
        }

        static public void ChangeVolume(byte Volume) {
            if (waveOut != null) {
                float volumeLevel = Mathf.Clamp01(Volume / 100f);
                waveOut.Volume = volumeLevel;
            }
        }

        static public void UpdateAudioSpatial() {
            if (waveOut != null && activeJukebox != null && mainCamera != null && isPlaying) {
                Vector3 playerPos = mainCamera.transform.position;
                Vector3 jukeboxPos = activeJukebox.transform.position;

                float distance = Mathf.Max(2.0f, Vector3.Distance(playerPos, jukeboxPos));
                float distanceRatio = Mathf.Clamp01((distance - 2.0f) / (maxDistance - 2.0f));
                float volume = Mathf.Pow(1 - distanceRatio, 10f);

                waveOut.Volume = volume * (activeJukeboxComp.volume.Value / 100f);

                Vector3 directionToPlayer = (playerPos - jukeboxPos).normalized;
                float pan = Vector3.Dot(directionToPlayer, mainCamera.transform.right);

                stereoProvider.LeftVolume = Mathf.Clamp01(1f + pan);
                stereoProvider.RightVolume = Mathf.Clamp01(1f - pan);
            }
        }
    }

    public class StereoVolumeSampleProvider : ISampleProvider {
        private readonly ISampleProvider source;
        private float leftVolume = 1.0f;
        private float rightVolume = 1.0f;

        public StereoVolumeSampleProvider(ISampleProvider source) {
            if (source.WaveFormat.Channels != 2) {
                throw new ArgumentException("Source sample provider must be stereo");
            }
            this.source = source;
        }

        public float LeftVolume {
            get { return leftVolume; }
            set { leftVolume = Math.Max(0, Math.Min(1, value)); }
        }

        public float RightVolume {
            get { return rightVolume; }
            set { rightVolume = Math.Max(0, Math.Min(1, value)); }
        }

        public WaveFormat WaveFormat {
            get { return source.WaveFormat; }
        }

        public int Read(float[] buffer, int offset, int count) {
            int samplesRead = source.Read(buffer, offset, count);
            for (int i = 0; i < samplesRead; i += 2) {
                buffer[offset + i] *= leftVolume;
                buffer[offset + i + 1] *= rightVolume;
            }
            return samplesRead;
        }
    }

}