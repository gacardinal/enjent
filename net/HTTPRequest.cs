using System.Collections.Generic;
using System.Collections.Specialized;

namespace NarcityMedia.Net
{
    /// <summary>
    /// Represents supported HTTP methods
    /// </summary>
    public enum HTTPMethod {
        GET, POST, DELETE, PUT
    }

    public class HTTPRequest
    {
        public string URL;
        public NameValueCollection QueryString;
        public Dictionary<string, string> Headers;
        public HTTPMethod Methods;
        
        public HTTPRequest(string url, HTTPMethod method, Dictionary<string, string> headers)
        {
            this.URL = url;
            this.Methods = method;
            this.Headers = headers;
            this.QueryString = new NameValueCollection(0);
        }

        public HTTPRequest(string url, HTTPMethod method, Dictionary<string, byte[]> headers)
        {
            this.URL = url;
            this.Methods = method;
            this.QueryString = new NameValueCollection(0);

            Dictionary<string, string> mappedHeaders = new Dictionary<string, string>(headers.Count);
            foreach (KeyValuePair<string, byte[]> header in headers)
            {
                mappedHeaders.Add(header.Key, System.Text.Encoding.Default.GetString(header.Value).Trim());
            }

            this.Headers = mappedHeaders;
        }

        public HTTPRequest(string url, HTTPMethod method, Dictionary<string, byte[]> headers, NameValueCollection queryString) : this(url, method, headers)
        {
            this.QueryString = queryString;
        }
    }
}
