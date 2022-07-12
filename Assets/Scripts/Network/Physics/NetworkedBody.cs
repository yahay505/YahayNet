

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityTools;
namespace Network
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkedBody : MonoBehaviour
    {
        [SerializeField] public float FlashWhenSend, FlashWhenUpdated;
        public float accumulated;
        AutoGet<Rigidbody> rb;

        public static ushort nextID;



        private void FixedUpdate()
        {
            OwnershipUpdate();
            if (accumulated>=0)
            {
                // accumulated++;
                accumulated += Time.fixedDeltaTime;
                accumulated+=rb.value.velocity.magnitude;
                // accumulated+=rb.value.;
                
            }

        }





        [SerializeField] public ushort ID;
        [SerializeField] public bool AutoID = true;
        [SerializeField]public int Authority = 0;
        [SerializeField] public bool Owned = false;

        public NetworkedBody()
        {
            rb = new(this);
        }

        #region ownership&authority
    
        // public void SetOwnership(int owner) => SetOwnership(this, owner);
        //
        // public static void SetOwnership(NetworkedBody networkedBody, int owner)
        // {
        //     networkedBody.Authority = owner;
        // }
        // public void SetAuthority(int owner) => SetOwnership(this, owner);
        //
        // public static void SetAuthority(NetworkedBody networkedBody, int owner)
        // {
        //     networkedBody.Authority = owner;
        // }

        private void OwnershipUpdate()
        {
            rb.value.isKinematic = !newNetworkManager.main.isConnected;

            if (rb.value.IsSleeping())
            {
                Authority = 0;
            }
            
        }
        private void OnCollisionEnter(Collision collision)
        {
            accumulated += collision.relativeVelocity.sqrMagnitude;
        }
        #endregion

        public NBodyUpdate GetRbUpdate()
        {
            FlashWhenSend = 1;
            accumulated = 0;
            return (new NBodyUpdate(ID, rb.value.position, rb.value.rotation,rb.value.IsSleeping(), rb.value.velocity,
                rb.value.angularVelocity));
        }

        public static Dictionary<ushort, NetworkedBody> allBodies = new Dictionary<ushort, NetworkedBody>();

        public static void ApplyUpdates(IEnumerable<NBodyUpdate> updates)
        {
            updates.ForEach(ApplyUpdate);
        }

        public static void ApplyUpdate(NBodyUpdate update)
        {
            var item = allBodies[update.ID];
                item.rb.get().position= update.pos;
                item.rb.get().rotation = update.rot;
            
            var sleep = update.sleep;
            if (sleep)
            {
                item.rb.value.Sleep();
            }
            else
            {
                    item.rb.get().velocity = update.Vels.linvel;
                    item.rb.get().angularVelocity = update.Vels.angVel;
            
            }
            item.FlashWhenUpdated = 1;
        }

        private static float flashTime = .25f;
        IEnumerator Flash()
        {
            while (true)
            {
                var clr=Color.Lerp(Authority == 0 ? new Color(0.1f, 0.1f, .1f) : UnityTools.HSVRGBConverter.ToRgb(Authority / 5.5f, .5, .9), Color.yellow,
                    Mathf.Clamp01(FlashWhenSend));
                var clrold=Color.Lerp(clr, Color.blue,Mathf.Clamp01( FlashWhenUpdated));
           
                GetComponent<MeshRenderer>().material.SetColor("_Color",clrold);
                FlashWhenSend -= Time.deltaTime/flashTime;
                FlashWhenUpdated -= Time.deltaTime/flashTime;

                yield return null;
            }
        }


        private void Start()
        {


            allBodies[ID] = this;
            StartCoroutine(Flash());
        }

        private void OnValidate()
        {
            if (AutoID&&ID==0)
            {
                ID = ++nextID;
            }
        }
    }

    public static class NBodyUtils
    {
        public static bool ShouldSet( float toSet, float value)
        
            =>!Mathf.Approximately(toSet,value);
        public static bool ShouldSet( Vector3 toSet, Vector3 value)
        
            => (!(Mathf.Approximately(toSet.x, value.x) &&
                  Mathf.Approximately(toSet.y, value.y) &&
                  Mathf.Approximately(toSet.z, value.z)));

        public static bool ShouldSet( Quaternion toSet, Quaternion value)
            =>
                (!(Mathf.Approximately(toSet.x, value.x) &&
                   Mathf.Approximately(toSet.y, value.y) &&
                   Mathf.Approximately(toSet.z, value.z) &&
                   Mathf.Approximately(toSet.w, value.w)));

        public static NBodyUpdate[] CreateManyUpdatePackets(this IEnumerable<NetworkedBody> a)
        {
            var x = 
                    a.Select(a => a.GetRbUpdate())
                .ToArray();
            // Debug.Log(x.ToHexString());
            return x;
        }

        public static NBodyUpdate readNBodyUpdate(this Packet packet)
        {

            var id = packet.ReadUShort();
            var pos = packet.ReadVector3();
            var rot = packet.ReadQuaternion();
            var sleep = packet.ReadBool();
            var linvel = sleep?default:packet.ReadVector3();
            var angvel = sleep?default:packet.ReadVector3();
            return new(id, pos, rot, sleep, linvel, angvel);
        }

        public static void Write(this Packet packet, NBodyUpdate nBodyUpdate)
        {

            packet.Write(nBodyUpdate.ID);
            packet.Write(nBodyUpdate.pos);
            packet.Write(nBodyUpdate.rot);
            packet.Write(nBodyUpdate.sleep);
            if (!nBodyUpdate.sleep)
            {
                packet.Write(nBodyUpdate.Vels.linvel);
                packet.Write(nBodyUpdate.Vels.angVel);
            
            }
        }

        public static Packet With(this Packet packet, NBodyUpdate nBodyUpdate)
        {
            Write(packet, nBodyUpdate);
            return packet;
        }

        // public static Vector3 ToF3(this Quaternion q) => new Vector3(q.x, q.y, q.z);
        // public static Quaternion FromF3(this Vector3 q) => new Quaternion(q.x, q.y, q.z,  Mathf.Sqrt(1 -(q.x*q.x + q.y*q.y + q.z*q.z)));
    }

    public struct NBodyUpdate
    {
        public static int size = 54;
        public ushort ID; //2
        public Vector3 pos; //12

        public Quaternion rot; //16

        // public Vector3 rotFirst3; //12
        public bool sleep;
        public vels Vels;//24

        public struct vels //24
        {
            public Vector3 linvel; //12
            public Vector3 angVel; //12
        }
        
        public NBodyUpdate(ushort id, Vector3 pos, Quaternion rot,bool sleep, Vector3 linvel, Vector3 angVel )
        {
            ID = id;
            this.pos = pos;
            this.rot = rot;
            this.sleep = sleep;
            Vels = new vels(){ linvel = linvel,angVel=angVel};
        }

        // public byte[] AsBytes()
        // {
        //     List<byte> bytes = new List<byte>(50);
        //     bytes.AddRange(BitConverter.GetBytes(ID));
        //     bytes.AddRange(BitConverter.GetBytes(pos.x));
        //     bytes.AddRange(BitConverter.GetBytes(pos.y));
        //     bytes.AddRange(BitConverter.GetBytes(pos.z));
        //     bytes.AddRange(BitConverter.GetBytes(rot.x));
        //     bytes.AddRange(BitConverter.GetBytes(rot.y));
        //     bytes.AddRange(BitConverter.GetBytes(rot.z));
        //     bytes.AddRange(BitConverter.GetBytes(rot.w));
        //     bytes.AddRange(BitConverter.GetBytes(sleep));
        //     if (!sleep)
        //     {
        //         bytes.AddRange(BitConverter.GetBytes(Vels.linvel.x));
        //         bytes.AddRange(BitConverter.GetBytes(Vels.linvel.y));
        //         bytes.AddRange(BitConverter.GetBytes(Vels.linvel.z));
        //         bytes.AddRange(BitConverter.GetBytes(Vels.angVel.x));
        //         bytes.AddRange(BitConverter.GetBytes(Vels.angVel.y));
        //         bytes.AddRange(BitConverter.GetBytes(Vels.angVel.z));
        //     }
        //     return bytes.ToArray();
        // }
    }
}