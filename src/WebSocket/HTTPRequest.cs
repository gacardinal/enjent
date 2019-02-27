using System.Collections.Generic;
using System.Collections.Specialized;

namespace NarcityMedia.Enjent
{
    /// <summary>
    /// Represents supported HTTP methods
    /// </summary>
    public enum EnjentHTTPMethod {
        GET, POST, DELETE, PUT
    }

    public sealed class EnjentHTTPRequest
    {
        public string URL;
        public NameValueCollection QueryString;
        public Dictionary<string, string> Headers;
        public EnjentHTTPMethod Methods;
        
        public EnjentHTTPRequest(string url, EnjentHTTPMethod method, Dictionary<string, string> headers)
        {
            this.URL = url;
            this.Methods = method;
            this.Headers = headers;
            this.QueryString = new NameValueCollection(0);
        }

        public EnjentHTTPRequest(string url, EnjentHTTPMethod method, Dictionary<string, byte[]> headers)
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

        public EnjentHTTPRequest(string url, EnjentHTTPMethod method, Dictionary<string, byte[]> headers, NameValueCollection queryString) : this(url, method, headers)
        {
            this.QueryString = queryString;
        }
    }
}
