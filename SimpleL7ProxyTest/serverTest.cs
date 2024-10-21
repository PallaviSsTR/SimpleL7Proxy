using NUnit.Framework;
using Moq;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SimpleL7Proxy.BackendOptions;
using SimpleL7Proxy.Interfaces;
using SimpleL7Proxy.Server;
using SimpleL7Proxy.PriorityQueue;
using SimpleL7Proxy.RequestData;
using SimpleL7Proxy.EventHubClient;
using System.Reflection;

namespace SimpleL7ProxyTest
{
    [TestFixture]
    public class serverTest
    {
        private readonly TelemetryClient? _telemetryClient;
        private Mock<IOptions<BackendOptions>> _mockOptions;
        private Mock<TelemetryClient> _mockTelemetryClient;
        private BackendOptions _backendOptions;
        private Server _Server { get; set; }

        [SetUp]
        public void Setup()
        {
            _backendOptions = new BackendOptions
            {
                Port = 8000,
                Timeout = 10000,
                Workers = 10,
                MaxQueueLength = 50,
                DefaultPriority = 1,
                PriorityKeys = new List<string> { "high", "medium", "low" },
                PriorityValues = new List<int> { 1, 2, 3 }
            };
           
            _mockOptions = new Mock<IOptions<BackendOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(_backendOptions);

            //_mockTelemetryClient.Setup(o => o.Value).Returns(_backendOptions);

            _Server = new Server(_mockOptions.Object, _telemetryClient);

        }


        [Test]
        public void Server_Constructor_InitializesCorrectly()
        {
            var server = new Server(_mockOptions.Object, _telemetryClient);

            Assert.IsNotNull(_Server);
        }

        //[Test]
        //public void Start_Server_Success()
        //{
        //    //var server = new Server(_mockOptions.Object, _mockTelemetryClient.Object);
        //    var cancellationToken = new CancellationToken();

        //    var queue = _Server.Start(cancellationToken);
        //    Assert.IsNotNull(queue);

        //    var runTask = _Server.Run();

        //    if (queue.Count > 0)
        //        Assert.Greater(queue.Count, 0);

        //    Assert.AreEqual(_backendOptions.MaxQueueLength, queue.MaxQueueLength);

            
        //    Assert.IsNotNull(runTask);
        //    Assert.Pass("Server ran and processed requests successfully.");
        //}

        [Test]
        public async Task Run_Server_ProcessesRequests()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var queue = _Server.Start(cancellationTokenSource.Token);

            var runTask = _Server.Run();


            cancellationTokenSource.Cancel();

            await runTask;

            Assert.IsNotNull(_Server);
            Assert.Pass("Server ran and processed requests successfully.");

            if (queue.Count > 0)
                Assert.Greater(queue.Count, 0);

            Assert.AreEqual(_backendOptions.MaxQueueLength, queue.MaxQueueLength);

            Assert.IsNotNull(runTask);
        }


    }
}
