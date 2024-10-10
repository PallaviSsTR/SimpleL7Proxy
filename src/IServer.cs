
using SimpleL7Proxy.PriorityQueue;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SimpleL7Proxy.IServer
{
    public interface IServer
    {
        Task Run();
        //BlockingCollection<RequestData> Start(CancellationToken cancellationToken);
        BlockingPriorityQueue<RequestData.RequestData> Start(CancellationToken cancellationToken);
    }
}
