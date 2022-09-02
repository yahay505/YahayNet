using Avatar;
using UnityEngine;
using UnityTools;

namespace Network.Player
{
    public class AvatarManager:UniversalComponent<AvatarManager>
    {
        public void SummonAvatar(Peer peer)
        {
            var obj=Resources.Load<GameObject>("Player");
            Instantiate(obj).GetComponent<AvatarController>().Setup(peer.ID);
        }
    }
}