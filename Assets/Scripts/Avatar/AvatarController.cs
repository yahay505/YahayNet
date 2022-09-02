using System;
using Network.Physics;
using UnityEngine;
using UnityTools;
using static UnityTools.EasyVec;

namespace Avatar
{
    // [RequireComponent(typeof(NetworkedBody))]
    public partial class AvatarController : MonoBehaviour
    {
        public GameObject localExt;
        public int peerID;
        private bool isLocalPLayer => peerID == Network.NetworkManager.main.PeerID;
        private Rigidbody rb;

        [SerializeField] private float speed;

        public void Setup(int peerID)
        {
            this.peerID = peerID;
            GetComponent<NetworkedBody>().Owned = true;
            GetComponent<NetworkedBody>().ID =(ushort) (1000+ peerID);
            GetComponent<NetworkedBody>().Authority = peerID;
            GetComponent<NetworkedBody>().Register();
        }

        // Start is called before the first frame update
        void Start()
        {

                Cursor.lockState = CursorLockMode.None;
            rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {

            if (isLocalPLayer)
            {
                Local_FixedUpdate();
            }
        }

        private void Update()
        {            
            GetComponent<NetworkedBody>().Authority = peerID;
            localExt.SetActive(isLocalPLayer);
            if (isLocalPLayer)
            {
                Local_Update();
            }
        }

        private void OnDrawGizmos()
        {
            if (isLocalPLayer)
            {
                Local_OnDrawGizmos();

            }
        }
    }
}