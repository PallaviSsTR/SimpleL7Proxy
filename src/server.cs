using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using SimpleL7Proxy.BackendOptions;
using SimpleL7Proxy.Interfaces;
using SimpleL7Proxy.IServer;
using SimpleL7Proxy.PriorityQueue;
using SimpleL7Proxy.RequestData;


// This class represents a server that listens for HTTP requests and processes them.
// It uses a priority queue to manage incoming requests and supports telemetry for monitoring.
// If the incoming request has the S7PPriorityKey header, it will be assigned a priority based the S7PPriority header.
namespace SimpleL7Proxy.Server
{
    public class Server : IServer.IServer
    {
        private IBackendOptions? _options;
        private readonly TelemetryClient? _telemetryClient; // Add this line
        private HttpListener httpListener;
        private CancellationToken _cancellationToken;
        //private BlockingCollection<RequestData> _requestsQueue = new BlockingCollection<RequestData>();
        private BlockingPriorityQueue<RequestData.RequestData> _requestsQueue = new BlockingPriorityQueue<RequestData.RequestData>();


        // Constructor to initialize the server with backend options and telemetry client.
        public Server(IOptions<BackendOptions.BackendOptions> backendOptions, TelemetryClient? telemetryClient)
        {
            if (backendOptions == null) throw new ArgumentNullException(nameof(backendOptions));
            if (backendOptions.Value == null) throw new ArgumentNullException(nameof(backendOptions.Value));

            _options = backendOptions.Value;
            _telemetryClient = telemetryClient;
            _requestsQueue.MaxQueueLength = _options.MaxQueueLength;

            var _listeningUrl = $"http://+:{_options.Port}/";

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(_listeningUrl);

            var timeoutTime = TimeSpan.FromMilliseconds(_options.Timeout).ToString(@"hh\:mm\:ss\.fff");
            Console.WriteLine($"Server configuration:  Port: {_options.Port} Timeout: {timeoutTime} Workers: {_options.Workers}");
        }

        // Method to start the server and begin processing requests.
        public BlockingPriorityQueue<RequestData.RequestData> Start(CancellationToken cancellationToken)
        {
            try
            {
                _cancellationToken = cancellationToken;
                httpListener.Start();
                Console.WriteLine($"Listening on {_options?.Port}");
                // Additional setup or async start operations can be performed here

                return _requestsQueue;
            }
            catch (HttpListenerException ex)
            {
                // Handle specific errors, e.g., port already in use
                Console.WriteLine($"Failed to start HttpListener: {ex.Message}");
                // Consider rethrowing, logging the error, or handling it as needed
                throw new Exception("Failed to start the server due to an HttpListener exception.", ex);
            }
            catch (Exception ex)
            {
                // Handle other potential errors
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw new Exception("An error occurred while starting the server.", ex);
            }
        }

        // Continuously listens for incoming HTTP requests and processes them.
        // Requests are enqueued with a priority based on specific headers.
        // The method runs until a cancellation is requested.
        // Each request is enqueued with a priority into BlockingPriorityQueue.
        public async Task Run()
        {
            if (_options == null) throw new ArgumentNullException(nameof(_options));

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Use the CancellationToken to asynchronously wait for an HTTP request.
                    var getContextTask = httpListener.GetContextAsync();
                    using (var delayCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken))
                    {
                        var delayTask = Task.Delay(Timeout.Infinite, delayCts.Token);

                        var completedTask = await Task.WhenAny(getContextTask, delayTask).ConfigureAwait(false);

                        // Cancel the delay task immedietly if the getContextTask completes first
                        if (completedTask == getContextTask)
                        {
                            delayCts.Cancel();
                            // _requestsQueue.Add(new RequestData(await getContextTask.ConfigureAwait(false)));
                            var rd = new RequestData.RequestData(await getContextTask.ConfigureAwait(false));
                            int priority = _options.DefaultPriority;
                            var priorityKey = rd.Headers.Get("S7PPriorityKey");
                            if (!string.IsNullOrEmpty(priorityKey) && _options.PriorityKeys.Contains(priorityKey)) //lookup the priority
                            {
                                var index = _options.PriorityKeys.IndexOf(priorityKey);
                                if (index >= 0)
                                {
                                    priority = _options.PriorityValues[index];
                                }
                            }
                            rd.Priority = priority;
                            rd.EnqueueTime = DateTime.UtcNow;

                            //_requestsQueue.Enqueue(new RequestData(await getContextTask.ConfigureAwait(false)));
                            if (!_requestsQueue.Enqueue(rd, priority))
                            {
                                Console.WriteLine("Failed to enqueue request. " + _requestsQueue.Count);

                                // send a 429 response
                                rd.Context.Response.StatusCode = 429;
                                rd.Context.Response.Headers["Retry-After"] = "500ms";
                                using (var writer = new System.IO.StreamWriter(rd.Context.Response.OutputStream))
                                {
                                    await writer.WriteAsync("Too many requests. Please try again later.");
                                }
                                rd.Context.Response.Close();
                                Console.WriteLine($"Pri: {priority} Stat: 429 Len: - {rd.Path}");

                            }
                        }
                        else
                        {
                            _cancellationToken.ThrowIfCancellationRequested(); // This will throw if the token is cancelled while waiting for a request.
                        }
                    }
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"An IO exception occurred: {ioEx.Message}");
                }
                catch (OperationCanceledException)
                {
                    // Handle the cancellation request (e.g., break the loop, log the cancellation, etc.)
                    Console.WriteLine("Operation was canceled. Stopping the listener.");
                    break; // Exit the loop
                }
                catch (Exception e)
                {
                    _telemetryClient?.TrackException(e);
                    Console.WriteLine($"Error: {e.Message}\n{e.StackTrace}");
                }
            }

            Console.WriteLine("Listener task stopped.");
        }
    }
}
