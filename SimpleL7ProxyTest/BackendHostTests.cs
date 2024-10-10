using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using NUnit;
using SimpleL7Proxy.BackendHost;
using System.Reflection;

namespace SimpleL7ProxyTest.BackendHostTest
{
    public class Tests
    {
        private Mock<BackendHost> _mockBackendHost;
        private Mock<Queue<double>> _mockPxLatency;
        private object _lockObj;
        private const int MaxData = 50;

        [SetUp]
        public void Setup()
        {
            _mockPxLatency = new Mock<Queue<double>>();
            _lockObj = new object();
            _mockBackendHost = new Mock<BackendHost>("http://localhost:3000", "/echo/resource?param1=sample", "");
            var type = typeof(BackendHost);
            type.GetField("PxLatency", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_mockBackendHost.Object, _mockPxLatency.Object);
            type.GetField("lockObj", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_mockBackendHost.Object, _lockObj);
        }
            

        [Test]
        public void AddPxLatency_EnqueueLatency_Positive()
        {
            // Arrange
            double latency = 123.45;

            // Act
            _mockBackendHost.Object.AddPxLatency(latency);

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
            errorsField.SetValue(_mockBackendHost.Object, 5);

            //Act
            _mockBackendHost.Object.AddError();

            // Assert
            int errors = (int)errorsField.GetValue(_mockBackendHost.Object);
            Assert.AreEqual(6, errors);
        }

        [Test]
        public void GetStatus_PxLatencyQueueEmpty_Positive()
        {
            //Act
            string status = _mockBackendHost.Object.GetStatus(out int calls, out int errorCalls, out double average);

            //Assert
            Assert.AreEqual(" - ", status);
        }

        [Test]
        public void GetStatus_PxLatencyQueueNotEmpty_Positive()
        {
            var type = typeof(BackendHost);
            var errorsField = type.GetField("errors", BindingFlags.NonPublic | BindingFlags.Instance);
            var _pxLatency = (Queue<double>)type.GetField("PxLatency", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_mockBackendHost.Object);

            // Set initial value of errors to 5
            errorsField.SetValue(_mockBackendHost.Object, 5);
            _pxLatency.Enqueue(124.08);
            _pxLatency.Enqueue(348.65);
            _pxLatency.Enqueue(276.67);


            //Act
            string status = _mockBackendHost.Object.GetStatus(out int calls, out int errorCalls, out double average);

            //Assert
            double[] latencies = { 124.08, 348.65, 276.67 };
            string expectedStatus = $" Calls: {3} Err: {5} Avg: {Math.Round(latencies.Average(), 3)}ms";
            Assert.AreEqual(expectedStatus, status);
        }

        [Test]
        public void AddLatency_ShouldAddLatencyToQueue_Positive()
        {
            // Arrange
            var latency = 100.0;

            // Act
            _mockBackendHost.Object.AddLatency(latency);

            // Use reflection to access the private 'latencies' field
            var latenciesField = typeof(BackendHost).GetField("latencies", BindingFlags.NonPublic | BindingFlags.Instance);
            var latenciesQueue = (Queue<double>)latenciesField.GetValue(_mockBackendHost.Object);

            // Assert
            Assert.AreEqual(1, latenciesQueue.Count);
            Assert.AreEqual(latency, latenciesQueue.Peek());
        }

        [Test]
        public void AddLatency_ShouldRemoveOldestLatencyWhenQueueIsFull_Positive()
        {
            // Arrange
            for (int i = 0; i < MaxData; i++)
            {
                _mockBackendHost.Object.AddLatency(i);
            }

            // Act
            var newLatency = 100.0;
            _mockBackendHost.Object.AddLatency(newLatency);

            // Use reflection to access the private 'latencies' field
            var latenciesField = typeof(BackendHost).GetField("latencies", BindingFlags.NonPublic | BindingFlags.Instance);
            var latenciesQueue = (Queue<double>)latenciesField.GetValue(_mockBackendHost.Object);

            // Assert
            Assert.AreEqual(MaxData, latenciesQueue.Count);
            Assert.AreEqual(1, latenciesQueue.Peek()); // The oldest latency (0) should be removed
            Assert.AreEqual(newLatency, latenciesQueue.ToArray()[MaxData - 1]); // The new latency should be at the end
        }

        [Test]
        public void AverageLatency_ShouldReturnZeroWhenNoLatencies()
        {
            // Act
            var averageLatency = _mockBackendHost.Object.AverageLatency();

            // Assert
            Assert.AreEqual(0.0, averageLatency);
        }

        [Test]
        public void AverageLatency_ShouldReturnCorrectAverageWithZeroSuccessRate()
        {
            //Arrange
            var type = typeof(BackendHost);
            var _latencies = (Queue<double>)type.GetField("latencies", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_mockBackendHost.Object);
            _latencies.Enqueue(100.0);
            _latencies.Enqueue(200.0);
            _latencies.Enqueue(300.0);

            //Act
            var averageLatency = _mockBackendHost.Object.AverageLatency();

            // Assert
            double[] latencies = { 100.0, 200.0, 300.0 };
            var expectedAverage = latencies.Average() + (1 - 0.0) * 100; // Using the mocked SuccessRate of 0.8
            Assert.AreEqual(expectedAverage, averageLatency);
        }

        [Test]
        public void AverageLatency_ShouldReturnCorrectAverageWithNonZeroSuccessRate()
        {
            //Arrange
            var type = typeof(BackendHost);
            var _latencies = (Queue<double>)type.GetField("latencies", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_mockBackendHost.Object);
            _latencies.Enqueue(100.0);
            _latencies.Enqueue(200.0);
            _latencies.Enqueue(300.0);
            var _callSuccess = (Queue<bool>)type.GetField("callSuccess", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_mockBackendHost.Object);
            _callSuccess.Enqueue(true);
            _callSuccess.Enqueue(true);
            _callSuccess.Enqueue(false);
            _callSuccess.Enqueue(true);
            _callSuccess.Enqueue(true);

            //Act
            var averageLatency = _mockBackendHost.Object.AverageLatency();

            // Assert
            double[] latencies = { 100.0, 200.0, 300.0 };
            var expectedAverage = latencies.Average() + (1 - 0.8) * 100; // Using the mocked SuccessRate of 0.8
            Assert.AreEqual(expectedAverage, averageLatency);
        }

        [Test]
        public void AddCallSuccess_ShouldRemoveOldestCallWhenQueueIsFull_Positive()
        {
            // Arrange
            for (int i = 0; i < MaxData; i++)
            {
                _mockBackendHost.Object.AddCallSuccess(i%2==0);
            }

            // Act
            bool newCallSuccess = false;
            _mockBackendHost.Object.AddCallSuccess(newCallSuccess);

            // Use reflection to access the private 'latencies' field
            var callSuccessField = typeof(BackendHost).GetField("callSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
            var callSuccessQueue = (Queue<bool>)callSuccessField.GetValue(_mockBackendHost.Object);

            // Assert
            Assert.AreEqual(MaxData, callSuccessQueue.Count);
            Assert.AreEqual(false, callSuccessQueue.Peek()); // The oldest latency (0) should be removed
            Assert.AreEqual(newCallSuccess, callSuccessQueue.ToArray()[MaxData - 1]); // The new latency should be at the end
        }
    }
}