﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools;
using UnityTools.Editor;

namespace Network.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public partial class NetworkedBody : MonoBehaviour
    {
        private float FlashWhenSend, FlashWhenUpdated;
        public float accumulated;
        Rigidbody rb;
        public bool AutoRegister;
        public static ushort nextID=256;
        [ReadOnly,SerializeField]private int authorityPropagationNo=0;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

        }

        private void Start()
        {
            if (AutoRegister)
            {
                Register();
            }
        }

        public void Register()
        {
            allBodies[ID] = this;
            StartCoroutine(Flash());
        }

        private void DeRegister()
        {
            allBodies.Remove(ID);
            StopAllCoroutines();
        }
        private void OnDisable()
        {
            DeRegister();
        }

        private void FixedUpdate()
        {
            

            OwnershipUpdate();
            if (accumulated>=0)
            {
                // accumulated++;
                accumulated += Time.fixedDeltaTime;
                accumulated+=rb.velocity.magnitude;
                // accumulated+=rb.value.;
                
            }

        }





        [SerializeField] public ushort ID;
        [SerializeField] public bool AutoID = true;

        [SerializeField] public int Authority { get; set; }



        [SerializeField] public bool Owned = false;



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
        private int sleepTicks;
        private void OwnershipUpdate()
        {
            rb.isKinematic = !NetworkManager.main.isConnected;

            changeTimer -= Time.fixedDeltaTime;
            if (rb.IsSleeping())
            {
                sleepTicks++;
                if (sleepTicks>100)
                {
                    if (TryChangeAuthority(0))
                    {
                        authorityPropagationNo = 0;
                    }

                    
                    
                }
            }
            else
            {
                sleepTicks = 0;
            }
            
        }
        
        private void OnCollisionEnter(Collision collision)
        {

                accumulated += collision.relativeVelocity.sqrMagnitude;
                if (collision.rigidbody&&collision.rigidbody.velocity.sqrMagnitude>rb.velocity.sqrMagnitude&&collision.rigidbody.TryGetComponent<NetworkedBody>(out var body))//if other is faster than us we change to their authority;
                {
                    if (body.Authority!=0&&body.authorityPropagationNo<255&&TryChangeAuthority(body.Authority))
                    {
                        accumulated += 100;
                        authorityPropagationNo = body.authorityPropagationNo + 1;
                    }

                    
                }
            

        }
        #endregion

        public bool TryChangeAuthority(int authority)
        {
            if (Owned)
            {
                return false;
            }
            if (changeTimer>0)
            {
                return false;
            }

            if (Authority==authority)
            {
                return false;

            }
            else
            {
                Authority = authority;
                changeTimer = 3;
                //emit message
                return true;
            }
        }
        private float changeTimer;

        private void OnValidate()
        {
            if (AutoID&&ID==0)
            {
                ID = ++nextID;
            }
        }
    }

}