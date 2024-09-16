using Unity.Netcode;
using Debug = UnityEngine.Debug;

namespace YTJukebox {
    public class YTNetworkManager : NetworkBehaviour {
        public static YTNetworkManager instance;


        void Awake() {
            instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestMessageServerRpc(string message) {
            SendMessageClientRpc(message);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message) {
            Debug.Log("Message received by all players: " + message);
        }
    }
}
