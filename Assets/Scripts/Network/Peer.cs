namespace Network
{
    public class Peer:ITransportRemoteClient
    {
        public Peer(int ID)
        {
            this.ID = ID;
        }
        public int ID { get; set; }
        public void Send(Packet packet, TransportMode transportMode)
        {
            throw new System.NotImplementedException();
        }
    }
}