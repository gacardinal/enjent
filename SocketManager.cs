using System.Collections.Generic;

namespace NarcityMedia
{
    /// <summary>
    /// Class that acts as a Proxy to manage the list of sockets.
    /// This pattern ensures the thread safety of the socket lists
    /// </summary>
    /// <remarks>See http://csharpindepth.com/Articles/General/Singleton.aspx</remarks>
    class SocketManager
    {
        private static SocketManager instance = new SocketManager();

        private Dictionary<string, ClientObject[]> url_sockets;

        private Dictionary<string, ClientObject> session_Socket;

        /// <summary>
        /// Associates multiple ClientObjects to a URL
        /// </summary>
        public Dictionary<string, ClientObject[]> URL_Sockets
        {
            get { return url_sockets; }
        }

        /// <summary>
        /// Associates a socket to a Lilium session string identifier
        /// </summary>
        public Dictionary<string, ClientObject> Session_Socket
        {
            get { return session_Socket; }
        }

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SocketManager()
        {
        }

        private SocketManager()
        {
            this.url_sockets = new Dictionary<string, ClientObject[]>(200);
            this.session_Socket = new Dictionary<string, ClientObject>(2000);
        }

        public static SocketManager Singleton
        {
            get
            {
                return instance;
            }
        }

        public void AddClientObject(ClientObject cli)
        {
            lock (this.url_sockets)
            {
                ;
            }

            lock (this.session_Socket)
            {
                ;
            }
        }
    }
}
