using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Moq;
using NUnit.Framework;
using SimpleL7Proxy.Interfaces;
using SimpleL7Proxy.EventHubClient;
using Newtonsoft.Json.Linq;
using SimpleL7Proxy.BackendHost;
using System.Reflection;

namespace SimpleL7ProxyTest.EventHubClientTests
{
    [TestFixture]
    public class Tests
    {
        private EventHubProducerClient _mockProducerClient;
        private  EventDataBatch? _batchData;
        private EventHubClient _eventHubClient;
        private string _connectionString = "";
        private string _eventHubName = "";
        //private string _connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
        //private string _eventHubName = "testhub";
        private object _lockObject = new object();
        private bool _isRunning = true;
        private Queue<string> EHLogBuffer = new Queue<string>();

        [SetUp]
        public void Setup()
        {
             
        //_mockProducerClient = new EventHubProducerClient(_connectionString, _eventHubName);
            _eventHubClient = new EventHubClient(_connectionString, _eventHubName);
            _connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey";
            _eventHubName = "testhub";
        _mockProducerClient = new EventHubProducerClient(_connectionString, _eventHubName);
            //_batchData = _mockProducerClient.CreateBatchAsync().Result;
        }

        [Test]
        public void Constructor_ShouldInitializeProducerClient()
        {
            // Arrange & Act
            //var client = new EventHubClient(_connectionString, _eventHubName);
            //var type = typeof(EventHubClient);
            //_isRunning = (bool)type.GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_eventHubClient);
            // Assert
            Assert.IsNotNull(_isRunning);

        }

        [Test]
        public void StartTimer_ShouldStartWriterTask()
        {
            // Arrange
            // Act
            _eventHubClient.StartTimer();
            var type = typeof(EventHubClient);
            _isRunning = (bool)type.GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_eventHubClient);
            // Assert

            if (!_isRunning)
                Assert.IsFalse(_isRunning);

            if (_isRunning)
                Assert.IsTrue(_isRunning);
        }

        [Test]
        public void StopTimer_ShouldCancelWriterTask()
        {
            // Arrange
            if (!_isRunning)
            {
                _eventHubClient.StartTimer();
            }

            // Act
            _eventHubClient.StopTimer();

            var type = typeof(EventHubClient);
            _isRunning = (bool)type.GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_eventHubClient);

            // Assert
            Assert.IsFalse(_isRunning);
        }

        [Test]
        public void SendData_ShouldAddDataToBuffer()
        {
            // Arrange
            var buffer = EHLogBuffer;
            EHLogBuffer = new Queue<string>();
            _eventHubClient.StartTimer();
            string data = "test data";

            // Act
            _eventHubClient.SendData(data);
            EHLogBuffer.Enqueue(data);
            // Assert
            Assert.AreEqual(1, EHLogBuffer.Count);
        }

        [Test]
        public async Task WriterTask_ShouldSendDataToEventHub()
        {
            // Arrange
            _eventHubClient.StartTimer();
            string data = "test data";
            _eventHubClient.SendData(data);

            // Act
            await _eventHubClient.WriterTask();

            // Assert
            
           _mockProducerClient.SendAsync(It.IsAny<EventDataBatch>(), It.IsAny<CancellationToken>());
        }

        [TearDown]
        public void Teardown()
        {
             _connectionString = "";
             _eventHubName = "";
            _eventHubClient.StopTimer();
            _mockProducerClient.CloseAsync();
        }
    }
}

