using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using NarcityMedia.Log;

namespace NarcityMedia.Net
{
    /// <summary>
    /// Acts as a decorator on the <see cref="HttpListener" /> class
    /// </summary>
    class HTTPServer
    {
        private HttpListener listener;
        private Uri rootEndpoint;

        /// <summary>
        /// Dictionnary keyed by (string) HTTP method to which a dictionnary of callbacks keyed by endpoint is associated
        /// </summary>
        private Dictionary<string, Dictionary<Uri, EndpointCallback>> methodEndpointsCallbackMap;

        /// <summary>
        /// List that holds all the registered endpoints as arrays of strings to avoid having to split them on every request
        /// </summary>
        private List<string[]> splitEndpoints;

        public delegate void EndpointCallback(HttpListenerRequest req, HttpListenerResponse res);

        public Uri RootEndpoint {
            get { return this.rootEndpoint; } 
            set {
                if (!this.listener.IsListening)
                {
                    this.rootEndpoint = value;
                }
                else
                {
                    throw new HttpListenerException();
                }
            }
        }

        public bool IsListening { get { return this.listener.IsListening; } }

        public HTTPServer(Uri endpoint)
        {
            this.rootEndpoint = endpoint;
            this.methodEndpointsCallbackMap = new Dictionary<string, Dictionary<Uri, EndpointCallback>>();
            this.methodEndpointsCallbackMap.Add("get", new Dictionary<Uri, EndpointCallback>());
            this.methodEndpointsCallbackMap.Add("post", new Dictionary<Uri, EndpointCallback>());
            this.methodEndpointsCallbackMap.Add("put", new Dictionary<Uri, EndpointCallback>());
            this.methodEndpointsCallbackMap.Add("patch", new Dictionary<Uri, EndpointCallback>());
            this.methodEndpointsCallbackMap.Add("delete", new Dictionary<Uri, EndpointCallback>());
            this.listener = new HttpListener();
            listener.Prefixes.Add(this.rootEndpoint.ToString());
        }

        public void Start()
        {
            Logger.Log("HTTP Server is starting to listen on endpoint: " + this.rootEndpoint, Logger.LogType.Success);
            this.listener.Start();

            while (this.listener.IsListening)
            {
                IAsyncResult result = this.listener.BeginGetContext(new AsyncCallback(this.HandleRequestAsync), this.listener);
                result.AsyncWaitHandle.WaitOne();
            }
        }

        private void HandleRequestAsync(IAsyncResult result)
        {
            HttpListener listener = (HttpListener) result.AsyncState;

            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                this.methodEndpointsCallbackMap[request.HttpMethod.ToLower()][request.Url](request, response);
            }
            catch (KeyNotFoundException)
            {
                SendResponse(response, HttpStatusCode.NotFound, "Not FOund");
            }
            catch
            {
                SendResponse(response, HttpStatusCode.InternalServerError, "Internal Server Error");
            }
        }

        public void SendResponse(HttpListenerResponse response, HttpStatusCode statusCode, string responseText)
        {
            SendResponse(response, (int) statusCode, responseText);
        }

        public void SendResponse(HttpListenerResponse response, int statusCode, string responseText)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            response.StatusCode = statusCode;
            response.AddHeader("Content-Type", "text/plain");

            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        // private EndpointCallback resolveURI(HttpListenerRequest req, string[] uriComponents) // '/send/notification/user'
        // {
        //     List<string[]> searchedSplitEndpoints = this.splitEndpoints;

        //     for (int i = 0; i < uriComponents.Length; i++)
        //     {
        //         searchedSplitEndpoints.RemoveAll(levels => levels[i] != uriComponents[i]);

        //         if (searchedSplitEndpoints.Count == 1)
        //         {
        //             return 
        //         }
        //     }

        //     return false;
        // }

        private void registerEndpoint(string method, string relativeEndpoint, EndpointCallback cb)
        {
            relativeEndpoint = relativeEndpoint.Substring(1, relativeEndpoint.Length - 1);
            Uri uri = new Uri(this.rootEndpoint.ToString() + relativeEndpoint);
            this.methodEndpointsCallbackMap[method].Add(uri, cb);
        }

        /// <summary>
        /// Attempts to mimic the 'Express.js' way to registers endpoint by associating an endpoint for the corresponding HTTP method with a delegate
        /// </summary>
        /// <param name="endpoint">The endpoint to register</param>
        /// <param name="cb">The delegate that will be called when the endpoint is contacted</param>
        public void Get(string endpoint, EndpointCallback cb)
        {
            this.registerEndpoint("get", endpoint, cb);
        }

        /// <see cref="Get()" />
        public void Post(string endpoint, EndpointCallback cb)
        {
            this.registerEndpoint("post", endpoint, cb);
        }

        /// <see cref="Get()" />
        public void Put(string endpoint, EndpointCallback cb)
        {
            this.registerEndpoint("put", endpoint, cb);
        }

        /// <see cref="Get()" />
        public void Patch(string endpoint, EndpointCallback cb)
        {
            this.registerEndpoint("patch", endpoint, cb);
        }

        /// <see cref="Get()" />
        public void Delete(string endpoint, EndpointCallback cb)
        {
            this.registerEndpoint("delete", endpoint, cb);
        }
    }
}
