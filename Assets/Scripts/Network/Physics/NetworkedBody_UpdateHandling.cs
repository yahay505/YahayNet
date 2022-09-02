using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;

namespace Network.Physics
{
    public partial class NetworkedBody
    {
        #region nBodyUpdate

        private float TimeSinceLastUpdate;

        public NBodyUpdate GetRbUpdate()
        {
            FlashWhenSend = 1;
            accumulated = 0;
            var update = new NBodyUpdate();
            update.ID = ID;
            if (rb.IsSleeping())
            {
                update.flags |= NBodyUpdate.UpdateFlags.isSleeping;
            }

            if (rb.isKinematic)
            {
                update.flags |= NBodyUpdate.UpdateFlags.isKinematic;
            }

            #region ownership

            // if (UncommitedAuthorityUpdate&&(Authority==0||Authority == NetworkManager.main.PeerID))
            // todo add optimisation later
            {
                update.flags |= NBodyUpdate.UpdateFlags.hasAuthorityInfo;
                update.Auth = new() {ID = Authority};
            }

            if (Owned)
            {
                update.flags |= NBodyUpdate.UpdateFlags.isOwned;
            }

            #endregion


            #region position

            //todo add ballistic projection and Vector Approximately cases 
            //todo delta compression
            update.flags |= NBodyUpdate.UpdateFlags.hasExactPositionInfo;
            update.Transforms = new() {pos = rb.position, rot = rb.rotation};

            #endregion


            #region velocity

            if (!rb.IsSleeping() && !rb.isKinematic)
            {
                update.flags |= NBodyUpdate.UpdateFlags.hasVelocityInfo;
                update.Vels = new() {linvel = rb.velocity, angVel = rb.angularVelocity};
            }

            #endregion

            return update;
        }

        public static Dictionary<ushort, NetworkedBody> allBodies = new Dictionary<ushort, NetworkedBody>();

        public static void ApplyUpdates(IEnumerable<NBodyUpdate> updates)
        {
            updates.ForEach(ApplyUpdate);
        }

        public static void ApplyUpdate(NBodyUpdate update)
        {
            if (!ShouldApplyUpdate(update))
            {
                return;
            }
            var item = allBodies[update.ID];

            var sleep = (update.flags & NBodyUpdate.UpdateFlags.isSleeping) != 0;
            var kinematic = (update.flags & NBodyUpdate.UpdateFlags.isKinematic) != 0;
            var hasVelInfo = (update.flags & NBodyUpdate.UpdateFlags.hasVelocityInfo) != 0;
            var hasExactPosition = (update.flags & NBodyUpdate.UpdateFlags.POSITION_MASK) ==
                                   NBodyUpdate.UpdateFlags.hasExactPositionInfo;
            var hasAuthortyInfo = (update.flags & NBodyUpdate.UpdateFlags.hasAuthorityInfo) != 0;
            var isOwned = (update.flags & NBodyUpdate.UpdateFlags.isOwned) != 0;

            if (hasExactPosition)
            {
                item.rb.position = update.Transforms.pos;
                item.rb.rotation = update.Transforms.rot;
            }

            if (hasVelInfo)
            {
                item.rb.velocity = update.Vels.linvel;
                item.rb.angularVelocity = update.Vels.angVel;
            }

            if (hasAuthortyInfo)
            {
                item.Authority = update.Auth.ID;
                Debug.Log($"tried set auth to {update.Auth.ID} in {update.ID} - {item.gameObject.name}");
            }

            if (kinematic != item.rb.isKinematic)
            {
                item.rb.isKinematic = kinematic;
            }

            if (sleep)
            {
                item.StopCoroutine(FSleep(5, null));

                item.StartCoroutine(FSleep(1, item.rb));
            }
            else
            {
                item.StopCoroutine(FSleep(5, null));
            }

            item.Owned = isOwned;


            item.FlashWhenUpdated = 1;

            IEnumerator FSleep(int i, Rigidbody rb)
            {
                while (i > 0)
                {
                    i--;
                    rb.Sleep();
                    yield return new WaitForFixedUpdate();
                }
            }
        }

        private static bool ShouldApplyUpdate(NBodyUpdate update)
        {
            var item = allBodies[update.ID];

            if (update.Auth.ID==NetworkManager.main.LocalPeer.ID&&item.Authority==update.Auth.ID)
            {
                return false;
            }

            return true;
        }

        #endregion

        private static float flashTime = .25f;

        private IEnumerator Flash()
        {
            while (true)
            {
                // var clr = Color.Lerp(
                //     Authority == 0
                //         ? new Color(0.1f, 0.1f, .1f)
                //         : UnityTools.HSVRGBConverter.ToRgb(Authority / 5.5f, .5, .9), Color.yellow,
                //     Mathf.Clamp01(FlashWhenSend));
                // var clrold = Color.Lerp(clr, Color.blue, Mathf.Clamp01(FlashWhenUpdated));
                //
                // GetComponent<MeshRenderer>().material.SetColor("_Color", clrold);
                // FlashWhenSend -= Time.deltaTime / flashTime;
                // FlashWhenUpdated -= Time.deltaTime / flashTime;
UnityEngine.Mathf.rgb
                yield return null;
            }
        }
    }
}