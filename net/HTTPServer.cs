using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace NarcityMedia.Net
{
    /// <summary>
    /// Acts as a decorator on the <see cref="HttpListener" /> public class
    /// </summary>
    public class HTTPServer
    {
        public delegate void EndpointCallback(HttpListenerRequest req, HttpListenerResponse res);
        public EndpointCallback on404;
        public EndpointCallback on500;

        private HttpListener listener;
        private Uri rootEndpoint;

        /// <summary>
        /// Dictionnary keyed by (string) HTTP method to which a dictionnary of callbacks keyed by endpoint is associated
        /// </summary>
        private Dictionary<string, Dictionary<Uri, EndpointCallback>> methodEndpointsCallbackMap;

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

            this.on404 = this.Default404;
            this.on500 = this.Default500;

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
                this.on404(request, response);
            }
            catch (Exception e)
            {
                this.on500(request, response);
            }
        }

        private void Default404(HttpListenerRequest req, HttpListenerResponse res)
        {
            SendResponse(res, HttpStatusCode.NotFound, "Not FOund");
        }

        private void Default500(HttpListenerRequest req, HttpListenerResponse res)
        {
            SendResponse(res, HttpStatusCode.InternalServerError, "Internal Server Error - ");
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

            SendBytes(response, buffer);
        }

        public void SendJSON(HttpListenerResponse response, int statusCode, string jsonString)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonString);
            response.ContentLength64 = buffer.Length;
            response.StatusCode = statusCode;
            response.AddHeader("Content-Type", "text/json");

            SendBytes(response, buffer);
        }

        private static void SendBytes(HttpListenerResponse response, byte[] buffer)
        {
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

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
