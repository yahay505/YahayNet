using UnityEngine;


    public class UIRelay : MonoBehaviour
    {
        public void StartAsServer() => NetworkManager.main.StartAsServer();
        public void StartAsClient()=> NetworkManager.main.StartAsClient();
    }