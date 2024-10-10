using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Options;
using SimpleL7Proxy.BackendHost;
using SimpleL7Proxy.BackendOptions;
using SimpleL7Proxy.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SimpleL7Proxy.Backends;
using System.Reflection;
using System.Net;
using NUnit.Framework.Interfaces;
using Castle.Core.Internal;

[TestFixture]
public class BackendsTests
{
    private Mock<IOptions<BackendOptions>> _mockOptions;
    private BackendOptions _backendOptions;
    private Mock<BackendHost> _mockBackendHost;
    private Mock<Backends> _backends;
    private CancellationTokenSource _cancellationTokenSource;

    [SetUp]
    public void Setup()
    {
        _mockBackendHost = new Mock<BackendHost>("http://localhost:3000", "/echo/resource?param1=sample", "");

        _backendOptions = new BackendOptions
        {
            Hosts = new List<BackendHost> { _mockBackendHost.Object },
            Client = new HttpClient(),
            PollInterval = 1000,
            PollTimeout = 1000,
            SuccessRate = 80,
            UseOAuth = false
        };
        _mockOptions = new Mock<IOptions<BackendOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_backendOptions);

        _backends = new Mock<Backends>(_mockOptions.Object);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    [TearDown]
    public void TearDown()
    {
        _cancellationTokenSource?.Dispose();
    }

    [Test]
    public void Start_ShouldInitializeBackendPoller()
    {
        // Act
        _backends.Object.Start(_cancellationTokenSource.Token);

        // Assert
        Assert.Pass("Backend poller started successfully.");
    }

    [Test]
    public void GetActiveHosts_ShouldReturnActiveHosts()
    {
        // Arrange
        bool newCallSuccess = true;
        _mockBackendHost.Object.AddCallSuccess(newCallSuccess);

        // Use reflection to access the private 'latencies' field
        var callSuccessField = typeof(BackendHost).GetField("callSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
        var callSuccessQueue = (Queue<bool>)callSuccessField.GetValue(_mockBackendHost.Object);
        _backends.Object.Start(_cancellationTokenSource.Token);

        // Act
        var activeHosts = _backends.Object.GetActiveHosts();

        // Assert
        Assert.IsNotNull(activeHosts);
        Assert.IsEmpty(activeHosts); // Assuming no active hosts initially
    }

    [Test]
    public void OAuth2Token_ShouldReturnCorrectToken()
    {
        // Arrange
        var token = "test_token";
        Azure.Core.AccessToken? newToken = new Azure.Core.AccessToken(token, DateTimeOffset.UtcNow.AddMinutes(5));
        var authTokenField = typeof(Backends).GetProperty("AuthToken", BindingFlags.NonPublic | BindingFlags.Instance);

        // Ensure the property is found
        Assert.IsNotNull(authTokenField, "AuthToken property not found.");

        // Ensure the instance type matches
        Assert.IsInstanceOf<Backends>(_backends.Object, "The instance type does not match.");
        authTokenField.SetValue(_backends.Object, newToken);

        // Act
        var result = _backends.Object.OAuth2Token();

        // Assert
        Assert.AreEqual(token, result);
    }

    [Test]
    public async Task waitForStartup_ShouldThrowExceptionIfNotStartedInTime()
    {
        // Arrange
        _cancellationTokenSource.CancelAfter(2000); // Cancel after 2 seconds

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _backends.Object.waitForStartup(1));
    }

    [Test]
    public async Task Run_ShouldUpdateHostStatusAndFilterActiveHosts()
    {
        // Arrange
        //_mockBackendHost.Setup(h => h.SuccessRate()).Returns(0.9);
        var type = typeof(BackendHost);
        var _callSuccess = (Queue<bool>)type.GetField("callSuccess", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_mockBackendHost.Object);
        _callSuccess.Enqueue(true);
        _callSuccess.Enqueue(true);
        _callSuccess.Enqueue(false);
        _callSuccess.Enqueue(true);
        _callSuccess.Enqueue(true);

        //Use Reflection to set the success rate to 0.8
        var _successRateField = typeof(Backends).GetField("_successRate", BindingFlags.NonPublic | BindingFlags.Static);
        _successRateField.SetValue(_backends.Object, 0.8);

        //Mock GetHostStatus method to return true
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var client = new HttpClient(mockHttpMessageHandler.Object);
        _backends.Setup(h=>h.GetHostStatus(It.IsAny<BackendHost>(), It.IsAny<HttpClient>())).ReturnsAsync(true);

        // Act
        var runTask = Task.Run(() => _backends.Object.Start(_cancellationTokenSource.Token));
        await Task.Delay(2000); // Let the Run method execute for a short time

        // Assert
        Assert.IsNotEmpty(_backends.Object.GetActiveHosts());
    }

}

