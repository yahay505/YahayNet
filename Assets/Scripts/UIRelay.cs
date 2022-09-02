using Network;
using UnityEngine;


    public class UIRelay : MonoBehaviour
    {
        public void StartAsServer() {
            Cursor.lockState = CursorLockMode.Locked;

            NetworkManager.main.SetupServer();
        }
        public void StartAsClient()
        {
            Cursor.lockState = CursorLockMode.Locked;

            NetworkManager.main.SetupClient();
        }
    }