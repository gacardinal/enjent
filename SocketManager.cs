using System.Collections.Generic;

namespace NarcityMedia
{
    class SocketManager
    {
        /// Hashtable that associates multiple sockets to a hashes narcity.com URL
        private Dictionary<string, ClientObject[]> _URL_Sockets;

        /// Hashtable that associates a socket to a Lilium session string identifier
        private Dictionary<string, ClientObject> _Session_Socket;

        public Dictionary<string, ClientObject[]> URL_Sockets
        {
            get { return _URL_Sockets; }
        }

        public Dictionary<string, ClientObject> Session_Socket
        {
            get { return _Session_Socket; }
        }

        public SocketManager()
        {
            this._URL_Sockets = new Dictionary<string, ClientObject[]>(200);
            this._Session_Socket = new Dictionary<string, ClientObject>(2000);
        }

        public void AddClientObject(ClientObject cli)
        {
            
        }
    }
}
