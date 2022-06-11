using System.Collections.Generic;


public class NetBuffer
{
    public Queue<NBodyUpdate[]> buffer = new(10);
    public void Enqueue(NBodyUpdate[] obj) => buffer.Enqueue(obj);
    public NBodyUpdate[] Dequeue() => buffer.Dequeue();
}
