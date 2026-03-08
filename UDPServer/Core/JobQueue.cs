using System.Collections.Concurrent;

namespace UDPServer.Core;

public class JobQueue
{
    //race condition을 피하기 위함
    private readonly BlockingCollection<IJob> _jobs = new BlockingCollection<IJob>();

    public void Enqueue(IJob job)
    {
        _jobs.Add(job);
    }

    public IJob Dequeue()
    {
        return _jobs.Take(); //pop이랑 비슷
    }
}