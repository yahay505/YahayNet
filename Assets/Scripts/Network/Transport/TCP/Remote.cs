using System.Net.Sockets;

namespace Network.Transport.TCP
{
    public class TCPRemoteServer : ITransportRemoteServer
    {

    }

    public class TCPRemoteClient : ITransportRemoteClient
    {
        public static TCPServer server;
        private readonly TcpClient tcpClient;
        public NetBuffer NetBuffer=new (100);
        private NetworkStream stream;
        public TCPRemoteClient(TcpClient tcpClient,int id)
        {
            this.tcpClient = tcpClient;

            ID = id;
            stream = this.tcpClient.GetStream();
        }

        public int  ID { get;  set; }
        public void Send(Packet packet, TransportMode transportMode)
        {
            var arr = packet.Pack();
            stream.WriteAsync(arr);
        }

        public bool connected()
        {
            var client = tcpClient.Client;
            return TCPUtils.CheckConnectedness(client);
        }


    }

    public static class TCPUtils
    {
        public static bool CheckConnectedness(Socket client)
        {
            bool blockingState = client.Blocking;
            try
            {
                byte[] tmp = new byte[1];

                client.Blocking = false;
                client.Send(tmp, 0, 0);
                return true;
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                client.Blocking = blockingState;
            }
        }
    }
}