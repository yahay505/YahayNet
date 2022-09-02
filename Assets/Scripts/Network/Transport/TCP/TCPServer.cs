using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityTools;

namespace Network.Transport.TCP
{
    public class TCPServer:ITransportServer
    {
        private readonly Dispatcher packetDispatcher = new();
        private TcpListener _tcp;
        private readonly Dictionary<int,TCPRemoteClient> _clients=new();
        private readonly Dictionary<int,Action<Packet,int>> _handlers=new();
        // private int NetFrame;
        public void Initialize()
        {
            _tcp = new(IPAddress.Any, 2500);
            _tcp.Start();
            TCPRemoteClient.server = this;
            RegisterPacketHandlerUnchecked(0, (packet,id)=>_clients[id].Send(new Packet(0).With(id),TransportMode.None));

        }



        public void TransportHouseKeeping()
        {
            CheckDisconnected();
            
        }

        public void PopBufferedPackets()
        {
            foreach (var (_, _cli) in _clients)
            {
                foreach (var packet in _cli.NetBuffer.Pop())
                {
                    handleMessage(packet,_cli);
                }
            }        }

        public void HandlePackets()
        {
            Profiler.BeginSample("TCP Dispatch");

            packetDispatcher.Dispatch();
            Profiler.EndSample();

        }

        public ITransportRemoteClient[] AcceptIncomingConnection(Func<int> IDGenerator)
        {
            var ret = new List<ITransportRemoteClient>();
            while (_tcp.Pending())
            {
                var client = _tcp.AcceptTcpClient();
                var id = IDGenerator();
                var remoteClient = new TCPRemoteClient(client,id);
                _clients.Add(id,remoteClient);
                ret.Add(remoteClient);
                // start receiver
                // ReSharper disable once AsyncVoidLambda
                new Task(async () =>
                {
                    var stream = client.GetStream();
                    while (true)
                    {
                        var lenghtBuffer = new byte[4];
                        await stream.ReadAsync(lenghtBuffer,0,4);
                        var datatBuffer = new byte[BitConverter.ToInt32(lenghtBuffer)];
                        await stream.ReadAsync(datatBuffer,0,BitConverter.ToInt32(lenghtBuffer));
                        packetDispatcher.Add(()=>handleMessage(datatBuffer,remoteClient));
                    }
                    // ReSharper disable once FunctionNeverReturns
                }, TaskCreationOptions.None).Start();
            }

            return ret.ToArray();
        }

        private void CheckDisconnected()
        {
            foreach (var (i, client) in _clients.Where(x=>!x.Value.connected()))
            {
                OnDisconnection.Invoke(i,client);
                _clients.Remove(i);
            }
        }

        private void handleMessage(byte[] dataBuff, TCPRemoteClient _client)
        {
            var packet = dataBuff.AsPacket();

            handleMessage( packet,_client);
        }

        private void handleMessage(Packet packet,TCPRemoteClient _client )
        {
            packet.ReadIDnFlags();
            if ((packet.Flags & PacketFlag.FrameInfo) != 0)
            {
                FlushNetworkFrame(_client);
                return;
            }

            if ((packet.Flags & PacketFlag.Buffer) != 0) //if packet needs buffer
            {
                _client.NetBuffer.Put(packet);
            }
            else
            {
                try
                {
                    _handlers[packet.ID](packet, _client.ID);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in TCP Handling packet with (id:{packet.ID},size:{packet.Length()}) \n Content:{packet.ToArray().ToHexString()} \n err:{e}");
                    // Debug.LogError($"Error in TCP Handling packet with (id:{packet.ID},size:{packet.Length()}) \n Content:{packet.ToArray().ToHexString()} ");

                    throw;
                }
            }
        }

        #region Sending
        
            public void Send(Packet packet, ITransportRemoteClient client_Arg, TransportMode transportMode)
            {
 
                var client=(TCPRemoteClient) client_Arg;// throw if not TCPRemoteClient
                client.Send(packet,transportMode);
            }

            public void SendToMany(Packet packet, IEnumerable<ITransportRemoteClient> clients, TransportMode transportMode)
            {
                foreach (var client in clients)
                {
                    client.Send(packet,transportMode);
                }
            }

            public void SendToAll(Packet packet, TransportMode transportMode)
            {
                foreach (var (_, client) in _clients)
                {
                    client.Send(packet,transportMode);
                }
            }


            #endregion

        void FlushNetworkFrame(TCPRemoteClient _client)
        {
            _client.NetBuffer.NextFrame();
        }


        #region PacketHandler

            public void RegisterPacketHandler(int id, Action<Packet,int> handler)
            {
                Contract.Assert(id>10);
                RegisterPacketHandlerUnchecked(id,handler);

            }
            private void RegisterPacketHandlerUnchecked(int id, Action<Packet, int> handler)
            {
                _handlers[id] = handler;

            }
            public void DeRegisterPacketHandler(int id)
            {
                _handlers.Remove(id);
            }
            


            public event Action<int,ITransportRemoteClient> OnDisconnection=(_,_)=>{};

        #endregion

        public ITransportRemoteClient GetClientByID(int id)
        {
            return _clients[id];
        }
    }
}