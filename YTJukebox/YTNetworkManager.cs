using Unity.Netcode;
using UnityEngine;
using YTJukeboxMod;

namespace YTJukebox
{
    public class YTNetworkManager : NetworkBehaviour
    {
        private int playersDownloaded = 0;
        public static YTNetworkManager instance;

        void Awake()
        {
            instance = this;
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
            if (input == true)
            {
                Audio.jukeboxList.Clear();
                Audio.jukeboxList = Audio.GetAllJukeboxes();
            }
            else
            {
                GameObject jukeBoxObject = ReturnObjectFromID(JukeboxID);
                Audio.jukeboxList.Clear();
                Audio.jukeboxList.Add(jukeBoxObject);
            }
        }

        private GameObject ReturnObjectFromID(ulong JukeboxID)
        {
            NetworkObject foundNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[JukeboxID];
            GameObject outputGameObject = foundNetworkObject.gameObject;
            return outputGameObject;
        }
    }
}
