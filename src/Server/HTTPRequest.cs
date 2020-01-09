using System.Collections.Generic;
using System.Collections.Specialized;

namespace NarcityMedia.Enjent.Server
{
    /// <summary>
    /// Represents supported HTTP methods
    /// </summary>
    public enum EnjentHTTPMethod {
        GET, POST, DELETE, PUT
    }

    /// <summary>
    /// A very 'bare-bones' representation of an HTTP request
    /// </summary>
    public sealed class EnjentHTTPRequest
    {
        public string URL;

        /// <summary>
        /// Query string that was sent with the request
        /// </summary>
        public NameValueCollection QueryString;

        /// <summary>
        /// HTTP headers sent with the request
        /// </summary>
        public Dictionary<string, string> Headers;

        /// <summary>
        /// HTTP method (or verb) used to make the request
        /// </summary>
        public EnjentHTTPMethod Method;
        
        public EnjentHTTPRequest(string url, EnjentHTTPMethod method, Dictionary<string, string> headers)
        {
            this.URL = url;
            this.Method = method;
            this.Headers = headers;
            this.QueryString = new NameValueCollection(0);
        }

        public EnjentHTTPRequest(string url, EnjentHTTPMethod method, Dictionary<string, byte[]> headers)
        {
            this.URL = url;
            this.Method = method;
            this.QueryString = new NameValueCollection(0);

            Dictionary<string, string> mappedHeaders = new Dictionary<string, string>(headers.Count);
            foreach (KeyValuePair<string, byte[]> header in headers)
            {
                mappedHeaders.Add(header.Key, System.Text.Encoding.Default.GetString(header.Value).Trim());
            }

            this.Headers = mappedHeaders;
        }

        public EnjentHTTPRequest(string url, EnjentHTTPMethod method, Dictionary<string, byte[]> headers, NameValueCollection queryString) : this(url, method, headers)
        {
            this.QueryString = queryString;
        }
    }
}
