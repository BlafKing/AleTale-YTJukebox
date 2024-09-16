using Unity.Netcode;
using UnityEngine;
using YTJukeboxMod;
using Debug = UnityEngine.Debug;

namespace YTJukebox {
    public class YTNetworkManager : NetworkBehaviour {
        private int playersDownloaded = 0;
        public static YTNetworkManager instance;

        void Awake() {
            instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void TriggerDownloadServerRpc(string inputURL, ulong JukeboxID) {
            if (!IsServer && !IsHost) {
                return;
            }

            BroadcastDownloadClientRpc(inputURL, JukeboxID);
        }

        [ClientRpc]
        private void BroadcastDownloadClientRpc(string inputURL, ulong JukeboxID) {
            StartDownloadFileAsync(inputURL, JukeboxID);
        }

        private async void StartDownloadFileAsync(string inputURL, ulong JukeboxID) {
            Debug.Log($"Download started on client for URL: {inputURL}");

            await Download.GetCustomSong(inputURL);

            NotifyDownloadCompleteServerRpc(JukeboxID);
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyDownloadCompleteServerRpc(ulong JukeboxID) {
            if (!IsServer && !IsHost) {
                return;
            }

            playersDownloaded++;

            if (playersDownloaded == PlayerManager.Instance.players.Count) {
                Debug.Log("All players have completed the download.");
                playersDownloaded = 0;

                NetworkObject jukeboxNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[JukeboxID];
                if (jukeboxNetworkObject != null) {
                    GameObject jukeboxGameObject = jukeboxNetworkObject.gameObject;
                    Audio.PlayCustomTrack(jukeboxGameObject);
                    
                }
            }
        }
    }
}
