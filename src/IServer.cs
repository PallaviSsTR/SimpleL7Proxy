
using SimpleL7Proxy.PriorityQueue;
using System.Collections.Concurrent;

namespace SimpleL7Proxy.IServer
{
    public interface IServer
    {
        Task Run();
        //BlockingCollection<RequestData> Start(CancellationToken cancellationToken);
        BlockingPriorityQueue<RequestData.RequestData> Start(CancellationToken cancellationToken);
    }
}
