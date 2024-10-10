using NUnit.Framework;
using System;
using System.Threading;
using SimpleL7Proxy.PriorityQueue;
using SimpleL7ProxyTest.BackendHostTest;
using Moq;

namespace SimpleL7ProxyTest
{
    [TestFixture]
    public class BlockingPriorityQueueTests
    {
        private Mock<BlockingPriorityQueue<int>> _mockQueue;

        [SetUp]
        public void Setup()
        {
            _mockQueue = new Mock<BlockingPriorityQueue<int>>();
            _mockQueue.Object.MaxQueueLength = 3;
        }

        [Test]
        public void Enqueue_ShouldAddItemToQueue()
        {
            // Arrange
            int item = 1;
            int priority = 1;

            // Act
            bool result = _mockQueue.Object.Enqueue(item, priority);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _mockQueue.Object.Count);
        }

        [Test]
        public void Enqueue_ShouldReturnFalseWhenQueueIsFull()
        {
            // Arrange
            _mockQueue.Object.Enqueue(1, 1);
            _mockQueue.Object.Enqueue(2, 2);
            _mockQueue.Object.Enqueue(3, 3);

            // Act
            bool result = _mockQueue.Object.Enqueue(4, 4);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(3, _mockQueue.Object.Count);
        }

        [Test]
        public void Dequeue_ShouldRemoveAndReturnHighestPriorityItem()
        {
            // Arrange
            _mockQueue.Object.Enqueue(1, 1);
            _mockQueue.Object.Enqueue(2, 2);
            _mockQueue.Object.Enqueue(3, 3);

            // Act
            int result = _mockQueue.Object.Dequeue(CancellationToken.None);

            // Assert
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, _mockQueue.Object.Count);
        }

        [Test]
        public void Dequeue_ShouldWaitForItemWhenQueueIsEmpty()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000); // Cancel after 1 second

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() => _mockQueue.Object.Dequeue(cancellationTokenSource.Token));
        }

        [Test]
        public void Enqueue_ShouldSignalDequeue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var dequeueTask = Task.Run(() => _mockQueue.Object.Dequeue(cancellationTokenSource.Token));

            // Act
            _mockQueue.Object.Enqueue(1, 1);

            // Assert
            Assert.AreEqual(1, dequeueTask.Result);
        }
    }
}

