using Unity.Netcode;
using UnityEngine;
using YTJukeboxMod;
using System.Collections.Generic;
using static SavedDevice;

namespace YTJukebox
{
    public class YTNetworkManager : NetworkBehaviour
    {
        private int playersDownloaded = 0;
        public static YTNetworkManager instance;
        public static bool skipStop;

        void Awake()
        {
            instance = this;
            skipStop = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void TriggerDownloadServerRpc(string inputURL, ulong JukeboxID)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }

            BroadcastDownloadClientRpc(inputURL, JukeboxID);
        }

        [ClientRpc]
        private void BroadcastDownloadClientRpc(string inputURL, ulong JukeboxID)
        {
            StartDownloadFileAsync(inputURL, JukeboxID);
        }

        private async void StartDownloadFileAsync(string inputURL, ulong JukeboxID)
        {
            Log.Info($"Download started on client for URL: {inputURL}");

            bool success = await Download.GetCustomSong(inputURL);

            NotifyDownloadCompleteServerRpc(JukeboxID, success);
        }

        [ClientRpc]
        public void StopTrackClientRpc()
        {
            Audio.StopCustomTrack();
        }

        [ClientRpc]
        public void ChangeVolumeClientRpc(byte v)
        {
            Audio.ChangeVolume(v);
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyDownloadCompleteServerRpc(ulong JukeboxID, bool success)
        {
            if (!IsServer && !IsHost || success == false)
            {
                return;
            }

            playersDownloaded++;

            if (playersDownloaded == PlayerManager.Instance.players.Count)
            {
                Log.Info("All players have completed the download.");
                playersDownloaded = 0;
                PlayCustomTrackClientRpc(JukeboxID);
            }
        }

        [ClientRpc]
        private void PlayCustomTrackClientRpc(ulong JukeboxID)
        {
            GameObject jukeboxGameObject = ReturnObjectFromID(JukeboxID);
            Audio.PlayCustomTrack(jukeboxGameObject);
        }

        [ClientRpc]
        public void SetSyncClientRpc(ulong JukeboxID, bool input)
        {
            List<GameObject> allJukeboxes = Audio.GetAllJukeboxes();
            skipStop = true;
            if (input == true)
            {
                foreach (GameObject jukeboxObject in allJukeboxes)
                {
                    Jukebox jukebox = jukeboxObject.GetComponent<Jukebox>();
                    jukebox.PlayerStopServerRpc();
                }
                foreach (GameObject jukeboxObject in Audio.jukeboxList)
                {
                    Jukebox jukebox = jukeboxObject.GetComponent<Jukebox>();
                    jukebox.PlayerPlayServerRpc(99);
                }
                Audio.jukeboxList.Clear();
                Audio.jukeboxList = allJukeboxes;
            }
            else
            {
                foreach (GameObject jukeboxObject in allJukeboxes)
                {
                    Jukebox jukebox = jukeboxObject.GetComponent<Jukebox>();
                    jukebox.PlayerStopServerRpc();
                }
                foreach (GameObject jukeboxObject in Audio.jukeboxList)
                {
                    Jukebox jukebox = jukeboxObject.GetComponent<Jukebox>();
                    jukebox.PlayerPlayServerRpc(99);
                }
                GameObject jukeBoxObject = ReturnObjectFromID(JukeboxID);
                Audio.jukeboxList.Clear();
                Audio.jukeboxList.Add(jukeBoxObject);
            }
            skipStop = false;
        }

        private GameObject ReturnObjectFromID(ulong JukeboxID)
        {
            NetworkObject foundNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[JukeboxID];
            GameObject outputGameObject = foundNetworkObject.gameObject;
            return outputGameObject;
        }
    }
}
