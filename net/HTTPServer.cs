using System;
using System.Net;
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
        private string endpoint;

        public string Endpoint {
            get { return this.endpoint; } 
            set {
                if (!this.listener.IsListening)
                {
                    this.endpoint = value;
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
                this.endpoint = endpoint;
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
            Logger.Log("HTTP Server is starting to listen on endpoint: " + this.endpoint, Logger.LogType.Success);
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

            response.AddHeader("Content-Type", "text/plain");
            string responseText = "ALLO";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer,0,buffer.Length);
            // You must close the output stream.
            output.Close();
        }
    }
}
