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
        private string rootEndpoint;

        /// <summary>
        /// Dictionnary keyed by (string) HTTP method to which a dictionnary of callbacks keyed by endpoint is associated
        /// </summary>
        private Dictionary<string, Dictionary<string, EndpointCallback>> methodEndpointsCallbackMap;

        /// <summary>
        /// List that holds all the registered endpoints as arrays of strings to avoid having to split them on every request
        /// </summary>
        private List<string[]> splitEndpoints;

        public delegate void EndpointCallback(HttpListenerRequest req, HttpListenerResponse res);

        public string Endpoint {
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

        public HTTPServer(string endpoint)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                this.rootEndpoint = endpoint;
                this.methodEndpointsCallbackMap = new Dictionary<string, Dictionary<string, EndpointCallback>>();
                this.methodEndpointsCallbackMap.Add("get", new Dictionary<string, EndpointCallback>());
                this.methodEndpointsCallbackMap.Add("post", new Dictionary<string, EndpointCallback>());
                this.methodEndpointsCallbackMap.Add("put", new Dictionary<string, EndpointCallback>());
                this.methodEndpointsCallbackMap.Add("patch", new Dictionary<string, EndpointCallback>());
                this.methodEndpointsCallbackMap.Add("delete", new Dictionary<string, EndpointCallback>());
                this.listener = new HttpListener();
                listener.Prefixes.Add(endpoint);
            }
            else
            {
                throw new ArgumentNullException("endpoint", "You must provide the constructor an endpoint for the HTTPServer to listen to");
            }
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
                this.methodEndpointsCallbackMap[request.HttpMethod.ToLower()]["/hello"](request, response);
            }
            catch (KeyNotFoundException)
            {
                // Throw 404
            }

            response.AddHeader("Content-Type", "text/plain");
            string responseText = "ALLO";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer,0,buffer.Length);
            // You must close the output stream.
            output.Close();
        }

        private bool resolveURI(string[] uriComponents) // '/send/notification/user'
        {
            List<string[]> searchedSplitEndpoints = this.splitEndpoints;

            for (int i = 0; i < uriComponents.Length; i++)
            {
                searchedSplitEndpoints.RemoveAll(levels => levels[i] != uriComponents[i]);
            }

            return false;
        }

        private void registerEndpoint(string method, string endpoint, EndpointCallback cb)
        {
            this.methodEndpointsCallbackMap[method].Add(endpoint, cb);
            this.splitEndpoints.Add(endpoint.Split('/'));
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
