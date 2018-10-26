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

        /// <summary>
        /// A list of all the rooms (one roomv per website endpoint)
        /// </summary>
        public List<Room> Rooms;

        /// <summary>
        /// A list of the website's current clients, keyed by their session identifier
        /// </summary>
        public Dictionary<string, ClientObject> Clients;

        /// <summary>
        /// A delegate that will be used by multiple Threads to 'schedule' jobs on the ClientObjects.
        /// This execution of the delegate will be thread safe because the code inside the delegate will not access
        /// shared data since.
        /// </summary>
        /// <param name="client"></param>
        public delegate void ClientObjectOperation(ClientObject client);

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

        private Room GetRoomByName(string roomName)
        {
            return this.Rooms.Find(name => name == roomName);
        }

        public static SocketManager Singleton
        {
            get
            {
                return instance;
            }
        }

        public bool AddClient(ClientObject client, string endpoint)
        {
            Room room = GetRoomByName(endpoint);
            
            if (room != null)
            {
                room.Clients.Add(client);
                return true;
            }
            
            return false;
        }

        public List<ClientObject> GetClientsByEndpoint(string endpoint)
        {
            List<ClientObject> clients = new List<ClientObject>();
            Room room = GetRoomByName(endpoint);            

            if (room != null)
            {
                clients = room.Clients;
            }

            return clients;
        }

        public bool RemoveClientByEndpoint(ClientObject client, string endpont)
        {
            Room room = GetRoomByName(endpoint);

            if (room != null)
            {
                return room.Clients.Remove(client);
            }

            return false;
        }
    }

    /// <summary>
    /// Represent a group of client object that should all receive the same WebSOcket events
    /// </summary>
    class Room
    {
        public string Name;

        public List<ClientObject> Clients;

        public Room(string name)
        {
            this.name = name;
            this.Clients = new List<ClientObject>(50);
        }
    }
}
