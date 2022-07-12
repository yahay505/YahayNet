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

        private List<Packet> _inc = new List<Packet>();

        public void Put(Packet p)
        {
            _inc.Add(p);
        }
        public void PutMany(IEnumerable<Packet> p)
        {
            _inc.AddRange(p);
        }

        public Packet[] Pop()
        {
            return buffer.Dequeue();
        }

        public void NextFrame()
        {
            buffer.Enqueue(_inc.ToArray());
            _inc = new List<Packet>();

        }
    }
}
