using Moq;
using NUnit.Framework;
using SimpleL7Proxy.BackendHost;
using SimpleL7Proxy.PriorityQueue;
using System;
using System.Reflection;

[TestFixture]
public class PriorityQueueItemComparerTests
{
    private Mock<PriorityQueueItemComparer<int>> _mockComparer;

    [SetUp]
    public void Setup()
    {
        _mockComparer = new Mock<PriorityQueueItemComparer<int>>();
    }

    [Test]
    public void Compare_ShouldReturnZero_WhenItemsAreNull()
    {
        // Act
        int result = _mockComparer.Object.Compare(null, null);

        // Assert
        Assert.AreEqual(0, result);
    }

    [Test]
    public void Compare_ShouldReturnPositive_WhenSecondItemIsNull()
    {
        // Arrange
        var item1 = new PriorityQueueItem<int>(1, 1);

        // Act
        int result = _mockComparer.Object.Compare(item1, null);

        // Assert
        Assert.AreEqual(1, result);
    }

    [Test]
    public void Compare_ShouldReturnNegative_WhenFirstItemHasLowerPriority()
    {
        // Arrange
        var item1 = new PriorityQueueItem<int>(1, 1);
        var item2 = new PriorityQueueItem<int>(1, 2);

        // Act
        int result = _mockComparer.Object.Compare(item1, item2);

        // Assert
        Assert.Less(result, 0);
    }

    [Test]
    public void Compare_ShouldReturnPositive_WhenFirstItemHasHigherPriority()
    {
        // Arrange
        var item1 = new PriorityQueueItem<int>(1, 2);
        var item2 = new PriorityQueueItem<int>(1, 1);

        // Act
        int result = _mockComparer.Object.Compare(item1, item2);

        // Assert
        Assert.Greater(result, 0);
    }

    [Test]
    public void Compare_ShouldReturnNegative_WhenItemsHaveSamePriorityButFirstIsOlder()
    {
        // Arrange
        var item1 = new PriorityQueueItem<int>(1, 1);
        Thread.Sleep(100);
        var item2 = new PriorityQueueItem<int>(1, 1);

        // Act
        int result = _mockComparer.Object.Compare(item1, item2);

        // Assert
        Assert.Less(result, 0);
    }

    [Test]
    public void Compare_ShouldReturnPositive_WhenItemsHaveSamePriorityButFirstIsNewer()
    {
        // Arrange

        var item2 = new PriorityQueueItem<int>(1, 1);
        Thread.Sleep(100);
        var item1 = new PriorityQueueItem<int>(1, 1);

        // Act
        int result = _mockComparer.Object.Compare(item1, item2);

        // Assert
        Assert.Greater(result, 0);
    }
}
