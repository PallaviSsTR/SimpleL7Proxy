using Moq;
using NUnit;
using SimpleL7Proxy.BackendHost;
using System.Reflection;

namespace SimpleL7ProxyTest.BackendHostTest
{
    public class Tests
    {
        private BackendHost _backendHost;
        private Mock<Queue<double>> _mockPxLatency;
        private object _lockObj;

        [SetUp]
        public void Setup()
        {
            _mockPxLatency = new Mock<Queue<double>>();
            _lockObj = new object();

            _backendHost = new BackendHost("http://localhost:3000", "/echo/resource?param1=sample", "");
            var type = typeof(BackendHost);
            type.GetField("PxLatency", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_backendHost, _mockPxLatency.Object);
            type.GetField("lockObj", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_backendHost, _lockObj);
        }
            

        [Test]
        public void AddPxLatency_EnqueueLatency_Positive()
        {
            // Arrange
            double latency = 123.45;

            // Act
            _backendHost.AddPxLatency(latency);

            // Assert
            var peek = new System.Collections.Generic.Queue<double>(_mockPxLatency.Object).ToArray();
            Assert.That(peek[0], Is.EqualTo(latency));

        }

        [Test]
        public void AddError_IncrementErrorCount_Positive()
        {

            var type = typeof(BackendHost);
            var errorsField = type.GetField("errors", BindingFlags.NonPublic | BindingFlags.Instance);

            // Set initial value of errors to 5
            errorsField.SetValue(_backendHost, 5);

            //Act
            _backendHost.AddError();

            // Assert
            int errors = (int)errorsField.GetValue(_backendHost);
            Assert.AreEqual(6, errors);
        }

        [Test]
        public void GetStatus_PxLatencyQueueEmpty_Positive()
        {
            //Act
            string status = _backendHost.GetStatus(out int calls, out int errorCalls, out double average);

            //Assert
            Assert.AreEqual(" - ", status);
        }

        [Test]
        public void GetStatus_PxLatencyQueueNotEmpty_Positive()
        {
            var type = typeof(BackendHost);
            var errorsField = type.GetField("errors", BindingFlags.NonPublic | BindingFlags.Instance);
            var _pxLatency = (Queue<double>)type.GetField("PxLatency", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_backendHost);

            // Set initial value of errors to 5
            errorsField.SetValue(_backendHost, 5);
            _pxLatency.Enqueue(124.08);
            _pxLatency.Enqueue(348.65);
            _pxLatency.Enqueue(276.67);


            //Act
            string status = _backendHost.GetStatus(out int calls, out int errorCalls, out double average);

            //Assert
            double[] latencies = { 124.08, 348.65, 276.67 };
            string expectedStatus = $" Calls: {3} Err: {5} Avg: {Math.Round(latencies.Average(), 3)}ms";
            Assert.AreEqual(expectedStatus, status);
        }
    }
}