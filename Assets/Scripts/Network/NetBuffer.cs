using System;
using System.Collections.Generic;

namespace Network
{
    public class NetBuffer
    {
        public Queue<Packet[]> buffer;

        
        private bool filled;
        public NetBuffer(int backFill)
         {
             buffer = new(backFill);
             for (int i = 0; i < backFill; i++)
             {
                 buffer.Enqueue(new Packet[]{});
             }
         }

        private List<Packet> _incoming= new List<Packet>();

        public void Put(Packet p)
        {
            _incoming.Add(p);
        }
        public void PutMany(IEnumerable<Packet> p)
        {
            _incoming.AddRange(p);
        }

        public Packet[] Pop()
        {
            return buffer.Dequeue();
        }

        public void NextFrame()
        {
            buffer.Enqueue(_incoming.ToArray());
            _incoming = new List<Packet>();

        }
    }
}
