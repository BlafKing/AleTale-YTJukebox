using Unity.Netcode;
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
        public void TriggerDownloadServerRpc(string inputURL, ServerRpcParams serverRpcParams = default) {
            if (!IsServer && !IsHost) {
                return;
            }

            BroadcastDownloadClientRpc(inputURL);
        }

        [ClientRpc]
        private void BroadcastDownloadClientRpc(string inputURL, ClientRpcParams clientRpcParams = default) {
            StartDownloadFileAsync(inputURL);
        }

        private async void StartDownloadFileAsync(string inputURL) {
            Debug.Log($"Download started on client for file: {inputURL}");

            await Download.GetCustomSong(inputURL);

            NotifyDownloadCompleteServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyDownloadCompleteServerRpc(ServerRpcParams serverRpcParams = default) {
            if (!IsServer && !IsHost) {
                return;
            }

            playersDownloaded++;

            if (playersDownloaded == PlayerManager.Instance.players.Count) {
                Debug.Log("All players have completed the download.");
                playersDownloaded = 0;
                Audio.PlayCustomTrack();
            }
        }
    }
}
