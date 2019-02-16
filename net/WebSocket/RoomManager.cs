using System;
using System.Collections;
using System.Collections.Generic;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        private List<WebSocketRoom> _rooms;
        private List<Client_Rooms> _Client_Rooms;

        public List<WebSocketRoom> Rooms
        {
            get
            {
                return this._rooms;
            }
        }

    }

    internal struct Client_Rooms
    {

    }
}


