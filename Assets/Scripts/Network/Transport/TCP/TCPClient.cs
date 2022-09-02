using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityTools;

namespace Network.Transport.TCP
{
    public class TCPClient : ITransportClient
    {
        private readonly Dispatcher packetDispatcher = new();
        private readonly Dictionary<int,Action<Packet>> _handlers=new();

        // public int ITransportClient.ID { get; protected set; }
        private TcpClient _tcp;
        private NetworkStream _stream;

        int ITransportClient.ID
        {
            get => id;
            set => id = value;
        }

        public bool connected => TCPUtils.CheckConnectedness(_tcp.Client);
        public void Initialize()
        {
            // todo add handlers
            RegisterPacketHandler(0,HandleHandshakeResponse);
        }
        
        public bool TryConnect()
        {

            try
            {
                // File.OpenWrite("")
                _tcp = new TcpClient();
                _tcp.Connect(IPAddress.Loopback, 2500);
                _stream = _tcp.GetStream();
                //start handler
                new Task(async () =>
                {
                    var stream = _stream;
                    while (true)
                    {
                        var lenghtBuffer = new byte[4];
                        await stream.ReadAsync(lenghtBuffer,0,4);
                        var datatBuffer = new byte[BitConverter.ToInt32(lenghtBuffer)];
                        await stream.ReadAsync(datatBuffer,0,BitConverter.ToInt32(lenghtBuffer));
                        packetDispatcher.Add(()=>handleMessage(datatBuffer));
                    }
                    
              
                    // ReSharper disable once FunctionNeverReturns
                }, TaskCreationOptions.None).Start();
                
                
                //handShake
                Handshake();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            
        }


        public void NewNetworkFrame(int frameNo)
        {
            var y = new Packet();
            y.Flags |= PacketFlag.FrameInfo;
            y.Write(frameNo);
            Send(y,TransportMode.None);
            Debug.Log("sent NewNetFrame");
        }

        #region handling Message
            public void HandlePackets()
            {
                Profiler.BeginSample("TCP Dispatch");

                packetDispatcher.Dispatch();
                Profiler.EndSample();
                
            }
            private void handleMessage(byte[] dataBuff)
            {
                var packet = dataBuff.AsPacket();

                handleMessage( packet);
            }

            private void handleMessage(Packet packet )
            {
                packet.ReadIDnFlags();
                try
                {
                    _handlers[packet.ID](packet);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in TCP Handling packet with (id:{packet.ID},size:{packet.Length()}) \n Content:{packet.ToArray().ToHexString()} \n err:{e}");
                    // Debug.LogError($"Error in TCP Handling packet with (id:{packet.ID},size:{packet.Length()}) \n Content:{packet.ToArray().ToHexString()} ");
                    throw;
                }
            
            }
        #endregion
        private void Handshake()
        {
            Send(new Packet(0),TransportMode.None);
        }

        private void HandleHandshakeResponse(Packet response)
        {
            id=response.ReadInt();
        }

        private bool wasConnected;
        private int id;

        public void TransportHouseKeeping()
        {
            if (!connected)
            {
                if (wasConnected)
                {
                    //Disconnected  
                    OnDisconnection.Invoke();
                    wasConnected = false;
                }
            }
            else
            {
                wasConnected = true;
            }
        }




        #region PacketHandler

            public void RegisterPacketHandler(int id, Action<Packet> handler)
            {
                _handlers[id] = handler;
            }

            public void DeRegisterPacketHandler(int id, Action<Packet> handler)
            {
                _handlers.Remove(id);
            }



            public event Action OnDisconnection=()=>{};

        #endregion



        public void Send(Packet packet, TransportMode transportMode)
        {
            _stream.WriteAsync(packet.Pack());
        }




    }
}