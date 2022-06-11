using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityTools;

[RequireComponent(typeof(Rigidbody))]
public class NetworkedBody : MonoBehaviour
{
    [SerializeField] public float FlashWhenSend, FlashWhenUpdated;
    public float accumulated;
    AutoGet<Rigidbody> rb;

    public ushort nextID
    {
        get
        {
            return (ushort) (
                FindObjectsOfType<NetworkedBody>()?.Max(x => x.ID) ?? 0
                + 1);
        }
    }


    private void FixedUpdate()
    {
        OwnershipUpdate();
        accumulated++;

    }





    public ushort ID { get; private set; }
    [SerializeField] public bool AutoID = true;
    public int Authority = 0;
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
            rb.value.isKinematic = !NetworkManager.main.isConnected;

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

    public byte[] Pack()
    {
        FlashWhenSend = 1;
        return new Packet().With(new NBodyUpdate(ID, rb.value.position, rb.value.rotation.ToF3(), rb.value.velocity,
            rb.value.angularVelocity)).Pack();
    }

    public static Dictionary<ushort, NetworkedBody> allBodies = new Dictionary<ushort, NetworkedBody>();

    public static void ApplyUpdate(NBodyUpdate update)
    {
        var item = allBodies[update.ID];
        item.rb.get().position = update.pos;
        item.rb.get().rotation = update.rotFirst3.FromF3();
        item.rb.get().velocity = update.linvel;
        item.rb.get().angularVelocity = update.angVel;
        item.FlashWhenUpdated = 1;
    }

    private static float flashTime = .2f;
    IEnumerator Flashsend()
    {
        while (true)
        {
            var clr=Color.Lerp(Authority == 0 ? new Color(0.1f, 0.1f, .1f) : UnityTools.HSVRGBConverter.ToRgb(Authority / 5.5f, .5, .9), Color.yellow,
                Mathf.Clamp01(FlashWhenSend));
            GetComponent<MeshRenderer>().material.SetColor("_Color",clr);
            FlashWhenSend -= Time.deltaTime/flashTime;
            yield return null;
        }
    }

    IEnumerator flashupdate()
    {
        while (true)
        {
            var clr=Color.Lerp(Authority == 0 ? new Color(0.1f, 0.1f, .1f) : UnityTools.HSVRGBConverter.ToRgb(Authority / 5.5f, .5, .9), Color.blue,Mathf.Clamp01( FlashWhenUpdated));
            GetComponent<MeshRenderer>().material.SetColor("_Color",clr);
            FlashWhenUpdated -= Time.deltaTime/flashTime;
            yield return null;
        }
    }
    private void Start()
    {
        if (ID == 0)
        {
            ID = nextID;
        }

        allBodies[ID] = this;
        StartCoroutine(flashupdate());
        StartCoroutine(Flashsend());
    }
}

public static class NBodyUtils
{
    public static byte[] Pack(this NetworkedBody[] a)
    {
        var x = BitConverter.GetBytes(a.Length).Concat(a.SelectMany(a => a.Pack())).ToArray();
        // Debug.Log(x.ToHexString());
        return x;
    }

    public static NBodyUpdate readNBodyUpdate(this Packet packet)
    {
        return new(
            packet.ReadUShort(),
            packet.ReadVector3(),
            packet.ReadVector3(),
            packet.ReadVector3(),
            packet.ReadVector3()
        );
    }

    public static void Write(this Packet packet, NBodyUpdate nBodyUpdate)
    {
        packet.Write(nBodyUpdate.ID);
        packet.Write(nBodyUpdate.pos);
        packet.Write(nBodyUpdate.rotFirst3);
        packet.Write(nBodyUpdate.linvel);
        packet.Write(nBodyUpdate.angVel);
    }

    public static Packet With(this Packet packet, NBodyUpdate nBodyUpdate)
    {
        Write(packet, nBodyUpdate);
        return packet;
    }

    public static Vector3 ToF3(this Quaternion q) => new Vector3(q.x, q.y, q.z);
    public static Quaternion FromF3(this Vector3 q) => new Quaternion(q.x, q.y, q.z, 1 - (q.x + q.y + q.z));
}

public struct NBodyUpdate
{
    public static int size = 50;
    public ushort ID; //2
    public Vector3 pos; //12
    public Vector3 rotFirst3; //12
    public Vector3 linvel; //12
    public Vector3 angVel; //12

    public NBodyUpdate(ushort id, Vector3 pos, Vector3 rotFirst3, Vector3 linvel, Vector3 angVel)
    {
        ID = id;
        this.pos = pos;
        this.rotFirst3 = rotFirst3;
        this.linvel = linvel;
        this.angVel = angVel;
    }
}