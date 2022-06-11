using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityTools;


public class NetworkManager : UniversalComponent<NetworkManager>
{
    public int acc;
    public Dictionary<int, NetworkedBody> Bodies = new();
    public List<NetworkedBody> OwnedList = new();
    public Dictionary<int,NetBuffer> Buffers= new();
    public bool isServer = false;
    public bool isStarted = false;
    private readonly Dispatcher dispatcher = new ();
    public int clientID;

    public bool isConnected
    {
        get
        {
            if (isServer)
            {
                return TcpClients.Count > 0;
            }
            else
            {
                return _clientTcp?.Connected??false;
            }
        }
    }

    private void Awake()
    {
        timebetween = Time.fixedDeltaTime * 10;
    }

    private void FixedUpdate()
    {
        dispatcher.Dispatch();
        if (isServer)
        {
            ServerCheckIncoming();
        }
        if (!isStarted||!isConnected)
        {
            return;
        }
        if (isConnected)
        {
            if (++acc==10)
            {
                acc = 0;
                if (isServer)
                {
                    foreach (var (client, buffer) in Buffers)
                    {
                        //Check and apply updates
                        try
                        {
                            ApplyUpdate(buffer.Dequeue(),client);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);

                            Console.WriteLine($"peer unhealthy! No updates from {client} ");

                        }
                    }

                    SendToClients(CreateServerUpdatePacket());

                }
                else
                {
                    // create and send update
                    SendToServer(CreateClientUpdatePacket());
                }
            }
        }
        
    }

    public void StartAsServer()
    {
        if (isStarted)
        {
            throw new Exception("network already started, cannot start again");
        }
        isStarted = true;
        isServer = true;
        ServerStart();
    }
    public void StartAsClient(){        
        if (isStarted)
        {
            throw new Exception("Network already started, cannot start again");
        }
        isStarted = true;
        isServer = false;

        ClientStart();
        
    }
    
    private static  float timebetween ;
    private const int targetKbps = 230;
    private const int bpp = 52 * 8;
    private static readonly byte[] updateMagick = BitConverter.GetBytes((ushort) 6);
    
    
    #region Server

        #region incoming

            

            public void ServerCheckIncoming(){
                if (!ServerTcp.Pending()) return;
                var id = nextID++;
                var client = ServerTcp.AcceptTcpClient();
                TcpClients.Add(id,client);
                // start receiver
                new Task(async () =>
                {
                    var stream = client.GetStream();
                    while (true)
                    {
                        var lenghtBuffer = new byte[4];
                        await stream.ReadAsync(lenghtBuffer,0,4);
                        var datatBuffer = new byte[BitConverter.ToInt32(lenghtBuffer)];
                        await stream.ReadAsync(datatBuffer,0,BitConverter.ToInt32(lenghtBuffer));
                        dispatcher.Add(()=>ServerHandleMessage(datatBuffer,id));
                    }
                }, TaskCreationOptions.None).Start();
                
            }
           
            void ServerHandleMessage(byte[] packet,int client)
            {
                ushort id = BitConverter.ToUInt16(packet[0..2]);
                ServersideHandlers[id](packet[2..],client);
            
            }
            
            private static readonly Dictionary<ushort, Action<byte[],int>> ServersideHandlers = new()
            {
                {0, (_,_) => { }},
                {1, (_,_) => { }},
                {2, (_,_) => { }},
                {3, (_,_) => { }},
                {4, (_,_) => { }},
                {5, (_,_) => { }},
                {6, (b, i) => { ServerBufferUpdate(UnpackNBodyUpdates(b), i);}},
            
            };



            
            
            static void ServerBufferUpdate(NBodyUpdate[] updates, int client)
            {
                try
                {
                    main.Buffers[client].Enqueue(updates);

                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    Console.WriteLine($"Cant keep up! too many updates from {client}");
                
                }
            }
        #endregion
        

        #region outgoing

          

            byte[] CreateServerUpdatePacket()
            {
                
                    
                return updateMagick.Concat( 
                    FindObjectsOfType<NetworkedBody>()
                        .Where(body => ( body.accumulated   > 0))
                        .OrderBy(body => body.accumulated)
                        .Take((int) Math.Floor(targetKbps * 1024 * timebetween  / bpp)).ToArray().
                        Pack()).ToArray();
                
            }
            void SendToClients(byte[] packet)
            {
                foreach (var client in TcpClients.Values)
                {
                    client.GetStream().WriteAsync(packet, 0, packet.Length);
                    Debug.Log($"S2A:{packet.ToHexString()}");
                }
            }

        #endregion
        public Dictionary<int,TcpClient> TcpClients = new ();
        public int nextID = 1;
        private TcpListener ServerTcp;
        public void ServerStart()
        {
            ServerTcp = new TcpListener(IPAddress.Any, 2500);
            ServerTcp.Start();
        }


    #endregion

    #region Client

        #region incoming

            void ClientStartListen()
            {
                new Task(async () =>
                {
                    var stream = _clientTcp.GetStream();
                    while (true)
                    {
                        var lenghtBuffer = new byte[4];
                        await stream.ReadAsync(lenghtBuffer,0,4);
                        var datatBuffer = new byte[BitConverter.ToInt32(lenghtBuffer)];
                        await stream.ReadAsync(datatBuffer,0,BitConverter.ToInt32(lenghtBuffer));
                        dispatcher.Add(()=>ClientHandleMessage(datatBuffer));
                    }
                }, TaskCreationOptions.None).Start();
            }
            
            void ClientHandleMessage(byte[] packet)
            {
                ushort id = BitConverter.ToUInt16(packet[0..2]);
                ClientsideHandlers[id](packet[2..]);
                
            }
                
            private static readonly Dictionary<ushort, Action<byte[]>> ClientsideHandlers = new()
            {
                {0, (_) => { }},
                {1, (_) => { }},
                {2, (_) => { }},
                {3, (_) => { }},
                {4, (_) => { }},
                {5, (_) => { }},
                {6, (b) => { ClientNbodyUpdate(UnpackNBodyUpdates(b));}},
                
            };

            static void ClientNbodyUpdate(NBodyUpdate[] updates)
            {
                ApplyUpdate(updates,0);
            }
            
            
            

        #endregion

        #region outgoing

        byte[] CreateClientUpdatePacket()
        {
            return updateMagick.Concat( 
                FindObjectsOfType<NetworkedBody>()
                    .Where(body => body.Authority==clientID&&( body.accumulated   > 0))
                    .OrderBy(body => body.accumulated)
                    .Take((int) Math.Floor(targetKbps * 1024 * timebetween  / bpp)).ToArray().
                    Pack()).ToArray();
        }

        void SendToServer(byte[] packet)
        {
            Debug.Log($"C2S :{packet.ToHexString()}");

            _clientTcp.GetStream().WriteAsync(packet, 0, packet.Length);
        }

        #endregion

        private TcpClient _clientTcp;
        public void ClientStart()
        {
            _clientTcp = new TcpClient();
            _clientTcp.Connect("127.0.0.1", 2500);
            
            ClientStartListen();
           

        }
    #endregion



    #region util

        
        static NBodyUpdate[] UnpackNBodyUpdates(byte[] bytes)
        {
            var packet = bytes.AsPacket();
            var lenght = packet.ReadUShort();
            NBodyUpdate[] updates = new NBodyUpdate[lenght];
            for (int i = 0; i < lenght; i++)
            {
                updates[i] = packet.readNBodyUpdate();
            }

            return updates;
            // NetworkManager.main.buffer(updates,client);
        }
        static void ApplyUpdate(NBodyUpdate[] updates, int client)
        {
            foreach (var update in updates)
            {
                        
                NetworkedBody.ApplyUpdate(update);
                        
            }
        }


    #endregion
}

