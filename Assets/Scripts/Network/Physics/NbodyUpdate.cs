using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Network.Physics
{

  public struct NBodyUpdate
    {
        public static int size = 54;
        public ushort ID; //2
        public UpdateFlags flags;

        public transform Transforms;
        public vels Vels;//24
        public authority Auth;

        public struct transform
        {
            public Vector3 pos; //12
            public Quaternion rot; //16
        }
        public struct vels //24
        {
            public Vector3 linvel; //12
            public Vector3 angVel; //12
        }
        public struct authority
        {
            public int ID;
        }
        // private NBodyUpdate(ushort id,UpdateFlags flags, Vector3 pos, Quaternion rot,bool sleep, Vector3 linvel, Vector3 angVel )
        // {
        //     ID = id;
        //     this.pos = pos;
        //     this.rot = rot;
        //     this.sleep = sleep;
        //     Vels = new vels(){ linvel = linvel,angVel=angVel};
        // }

        [Flags]
        public enum UpdateFlags : ushort
        {
            isSleeping=1<<0,
            isKinematic=1<<1,
            hasVelocityInfo=1<<2,
            // ReSharper disable once ShiftExpressionZeroLeftOperand
            hasExactPositionInfo=00<<3,
            hasDeltaPositionInfo=01<<3,
            positionReferLast=0b10<<3,
            positionPerfectBallistic=0b11<<3,
            hasAuthorityInfo=1<<5,
            isOwned=1<<6,
            fullyCompressed=1<<13,
            hasPerFieldCompression=1<<14,
            hasAdditionalFlags=1<<15,
            
            POSITION_MASK=0b11<<3,

        }
        
    }

  public static class NBodyUtils
  {
       

      public static NBodyUpdate[] CreateManyUpdatePackets(this IEnumerable<NetworkedBody> a)
      {
          var x = a.Select(a => a.GetRbUpdate())
              .ToArray();
          // Debug.Log(x.ToHexString());
          return x;
      }

      public static NBodyUpdate readNBodyUpdate(this Packet packet)
      {
          var nBodyUpdate = new NBodyUpdate();
           nBodyUpdate.ID = packet.ReadUShort();
           nBodyUpdate.flags = (NBodyUpdate.UpdateFlags) packet.ReadUShort();
          
           
           if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.hasExactPositionInfo)
           {
               nBodyUpdate.Transforms.pos= packet.ReadVector3();
               nBodyUpdate.Transforms.rot= packet.ReadQuaternion();
           }          
           if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.hasDeltaPositionInfo)
           {
               throw new NotImplementedException();
           }          
           if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.positionReferLast)
           { 
               // Do Nothing
           }          
           if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.positionPerfectBallistic)
           {
               // Do Nothing
           }
           if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.hasVelocityInfo)!=0)
           {
               nBodyUpdate.Vels.linvel=packet.ReadVector3();
               nBodyUpdate.Vels.angVel=packet.ReadVector3();
           }

           if ((nBodyUpdate.flags&NBodyUpdate.UpdateFlags.hasAuthorityInfo)!=0)
           {
               nBodyUpdate.Auth.ID=packet.ReadInt();
           }

           return nBodyUpdate;
      }

      public static void Write(this Packet packet, NBodyUpdate nBodyUpdate)
      {

          packet.Write(nBodyUpdate.ID);
          packet.Write((ushort)nBodyUpdate.flags);
          
          
          if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.hasExactPositionInfo)
          {
              packet.Write(nBodyUpdate.Transforms.pos);
              packet.Write(nBodyUpdate.Transforms.rot);
          }          
          if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.hasDeltaPositionInfo)
          {
              throw new NotImplementedException();
          }          
          if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.positionReferLast)
          { 
              // Do Nothing
          }          
          if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.POSITION_MASK)==NBodyUpdate.UpdateFlags.positionPerfectBallistic)
          {
                // Do Nothing
          }
          if ((nBodyUpdate.flags & NBodyUpdate.UpdateFlags.hasVelocityInfo)!=0)
          {
              packet.Write(nBodyUpdate.Vels.linvel);
              packet.Write(nBodyUpdate.Vels.angVel);
          }

          if ((nBodyUpdate.flags&NBodyUpdate.UpdateFlags.hasAuthorityInfo)!=0)
          {
              packet.Write(nBodyUpdate.Auth.ID);
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


    
    
}