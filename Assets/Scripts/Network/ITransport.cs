
    using System;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using UnityTools;

    namespace Network
    {
        public interface ITransport
        {

            public void Initialize();
            // public void SetNetworkFrame(int frameNo);
            /**
             * Handles housekeeping 
             */
            public void TransportHouseKeeping();

            /**
             * Handles all packets scheduled
             */
            public void HandlePackets();
        }
        public interface ITransportServer:ITransport
        {
            /**
             *  schedules 1 buffer frame worth packets to be handled
             */
            public void PopBufferedPackets();
            /**
             * Accepts All incoming connections assigns IDs from generator and returns all created Clients
             */
            public ITransportRemoteClient[] AcceptIncomingConnection(Func<int> IDGenerator);
            public void Send(Packet packet, ITransportRemoteClient client_Arg, TransportMode transportMode);
            public void SendToMany(Packet packet, IEnumerable<ITransportRemoteClient> clients, TransportMode transportMode);
            public void SendToAll(Packet packet, TransportMode transportMode);
            // public ITransportRemoteClient GetClientByID(int id);
            public void RegisterPacketHandler(int id, Action<Packet,int> handler);
            
            public void DeRegisterPacketHandler(int id);
            // public void DeRegisterAllPacketHandlers(int id);
            public event Action<int,ITransportRemoteClient> OnDisconnection;
        }
        public interface ITransportClient:ITransport
        {
            public int ID { get; protected set; }
            bool connected { get; }
            public void Send(Packet packet, TransportMode transportMode);
            public void RegisterPacketHandler(int id, Action<Packet> handler);
            public void DeRegisterPacketHandler(int id, Action<Packet> handler);
            // public void DeRegisterAllPacketHandlers(int id);
            public bool TryConnect();

            public void NewNetworkFrame(int frameNo);
            

            public event Action OnDisconnection;

        }
        public interface ITransportRemoteClient
        {
            
            public int ID {  get;  set; }
            public void Send(Packet packet, TransportMode transportMode);


        }
        public interface ITransportRemoteServer
        {
        
        }
[Flags]
public enum TransportMode
        {
            None=0,
            ReqAck=1,
            Ordered=2,
            ReqNetTick=4,
        }
    }
