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
            Debug.Log("RequestMessageServerRpc: " + message);
            SendMessageClientRpc(message);
        }

        [ClientRpc]
        private void SendMessageClientRpc(string message) {
            Debug.Log("SendMessageClientRpc: " + message);
        }
    }
}
