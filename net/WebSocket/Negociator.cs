using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;

namespace NarcityMedia.Net
{
    public partial class WebSocketClient : IDisposable {
        
        private bool AppendHeaderChunk(byte[] buffer, int byteRead)
        {
            if (this.writeindex + byteRead <= WebSocketClient.HEADER_CHUNK_BUFFER_SIZE)
            {
                for (int i = 0; i < byteRead; i++)
                {
                    this.requestheaders[i + this.writeindex] = buffer[i];
                }

                this.writeindex += byteRead;
                this.requestheaderslength += byteRead;

                return true;
            } 
                
            return false;  
        }


        /// <summary>
        /// Here until we implement actual lmlTk as client key
        /// </summary>
        private static string GenerateRandomToken(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 1; i < size+1; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            if (lowerCase)
                return builder.ToString().ToLower();
            else
                return builder.ToString();
        }

        public bool ReadRequestHeaders()
        {
            byte[] buffer = new byte[HEADER_CHUNK_BUFFER_SIZE];
            int byteRead = 0;

            do
            {
                byteRead = this.socket.Receive(buffer);

                if (!this.AppendHeaderChunk(buffer, byteRead))
                {
                    return false;
                }
            }
            while ( byteRead != 0 && buffer[byteRead - 1] != NEWLINE_BYTE && buffer[byteRead - 2] != NEWLINE_BYTE );

            return true;
        }

        public bool AnalyzeRequestHeaders()
        {
            bool lookingForQuestionMark = true;
            bool buildingQueryString = false;
            int urlStartImdex = 0;
            // First, read until new line to get method and path
            for (int i = 0; i < this.requestheaderslength; i++)
            {
                if (this.requestheaders[i] == WebSocketClient.NEWLINE_BYTE)
                {
                    this.methodandpath = new byte[i];
                    Array.Copy(this.requestheaders, 0, this.methodandpath, 0, i);                    

                    break;
                }
                else if (lookingForQuestionMark && this.requestheaders[i] == WebSocketClient.QUESTION_MARK_BYTE)
                {
                    urlStartImdex = i + 1; // +1 to ignore the '?'
                    lookingForQuestionMark = false;
                    buildingQueryString = true;
                }
                else if (buildingQueryString && this.requestheaders[i] == WebSocketClient.SPACE_BYTE)
                {
                    this.url = new byte[i - urlStartImdex];
                    Array.Copy(this.requestheaders, urlStartImdex, this.url, 0, i - urlStartImdex);
                    this.currentUrl = System.Text.Encoding.Default.GetString(this.url);
                    buildingQueryString = false;
                }
            }
            
            // If no '? was found request is invalid
            if (lookingForQuestionMark)
            {
                this.socket.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 401\n"));
                return false;
            }

            // Read until ":", then read until "new line"
            int lastStop = this.methodandpath.Length + 1;
            byte lookingFor = WebSocketClient.COLON_BYTE;
            bool trimming = true;
            bool lookingForColon = true;
            string currentheader = String.Empty;
            for (int i = lastStop; i < this.requestheaderslength; i++)
            {
                if (trimming && this.requestheaders[i] == WebSocketClient.SPACE_BYTE)
                {
                    lastStop++;
                    continue;
                }
                else
                {
                    trimming = false;
                }

                if (this.requestheaders[i] == lookingFor)
                {
                    int len = i - lastStop;
                    byte[] subset = new byte[len];
                    Array.Copy(this.requestheaders, lastStop, subset, 0, len);
                    
                    if (lookingForColon)
                    {
                        currentheader = System.Text.Encoding.Default.GetString(subset);
                    }
                    else
                    {
                        this.headersmap[currentheader] = subset;
                    }

                    lookingFor = lookingForColon ? NEWLINE_BYTE : COLON_BYTE;
                    lookingForColon = !lookingForColon;
                    lastStop = i + 1;
                    trimming = true;
                }
            }

            return true;
        }

        public bool Negociate101Upgrade()
        {
            if (this.headersmap.ContainsKey(WebSocketClient.WEBSOCKET_COOKIE_HEADER))
            {
                String cookieData = System.Text.Encoding.Default.GetString(this.headersmap[WebSocketClient.WEBSOCKET_COOKIE_HEADER]);
                string cookieName = "lmltk=";
                int index = cookieData.IndexOf(cookieName);
                if (index != -1)
                {
                    int nextColon = cookieData.IndexOf(';', index + cookieName.Length);
                    string lmlTk = (nextColon != -1) ? cookieData.Substring(index + cookieName.Length, nextColon - index + cookieName.Length) 
                                                    : cookieData.Substring(index + cookieName.Length);

                    this.lmlTk = lmlTk;
                }
            }

            if (this.headersmap.ContainsKey(WebSocketClient.WEBSOCKET_SEC_KEY_HEADER))
            {
                byte[] seckeyheader = this.headersmap[WebSocketClient.WEBSOCKET_SEC_KEY_HEADER];
                byte[] tohash = new byte[seckeyheader.Length + WebSocketClient.RFC6455_CONCAT_GUID.Length - 1];
                for (int i = 0; i < seckeyheader.Length - 1; i++)
                {
                    tohash[i] = seckeyheader[i];
                }
                for (int i = 0; i < WebSocketClient.RFC6455_CONCAT_GUID.Length; i++) 
                {
                    tohash[i + seckeyheader.Length - 1] = WebSocketClient.RFC6455_CONCAT_GUID[i];
                }

                string negociatedkey;
                using (SHA1Managed sha1 = new SHA1Managed()) {
                    var hash = sha1.ComputeHash(tohash);
                    negociatedkey = Convert.ToBase64String(hash);
                }

                this.socket.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 101 Switching Protocols\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Connection: upgrade\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Upgrade: websocket\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Sec-WebSocket-Accept: " + negociatedkey));

                if (this.headersmap.ContainsKey("Sec-WebSocket-Protocol"))
                {
                    byte[] protocol = new byte[0];
                    this.headersmap.TryGetValue("Sec-WebSocket-Protocol", out protocol);
                    this.socket.Send(System.Text.Encoding.Default.GetBytes("Sec-WebSocket-Protocol: " + protocol));
                }

                this.socket.Send(System.Text.Encoding.Default.GetBytes("\n\n"));
            
                return true;
            }

            return false;
        }

    }
}
