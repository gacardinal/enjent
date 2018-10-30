using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NarcityMedia.Net;
using System.Runtime.Serialization;

namespace NarcityMedia
{
    /// <summary>
    /// public class that acts as a Proxy to manage the list of sockets.
    /// This pattern ensures the thread safety of the socket lists
    /// </summary>
    /// <remarks>See http://csharpindepth.com/Articles/General/Singleton.aspx</remarks>
    public class SocketManager
    {
        private static readonly SocketManager instance = new SocketManager();

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
            this.Rooms = new List<Room>(60);
            this.Clients = new Dictionary<string, ClientObject>(2000);
        }

        /// <summary>
        /// Returns the SocketManager singleton instance
        /// </summary>
        /// <value>Singleton instance</value>
        public static SocketManager Instance
        {
            get
            {
                return instance;
            }
        }

        public Room GetRoomByName(string name)
        {
            return this.Rooms.Find(room => room.Name == name);
        }

        /// <summary>
        /// Returns a copy of the Rooms list
        /// </summary>
        /// <returns>A copy (thread safe) of the Rooms lists</returns>
        public List<Room> GetRooms()
        {
            return this.Rooms;
        }

        /// <summary>
        /// Returns a COPY of the clients as a  list
        /// </summary>
        /// <returns>A copy (thread safe) of the clients as a list</returns>
        public List<ClientObject> GetClients()
        {
            return this.Clients.Values.ToList();
        }

        /// <summary>
        /// Adds a given client to a specified room instance.
        /// If the room can't be found, it will be created.
        /// </summary>
        /// <param name="client">The client object to insert</param>
        public bool AddClient(ClientObject client)
        {
            lock (this.Rooms)
            {
                Room room = GetRoomByName(client.currentUrl);
                
                if (room == null)
                {
                    room = new Room(client.currentUrl);
                    this.Rooms.Add(room);
                }

                room.Clients.Add(client);
            }

            lock (this.Clients)
            {
                try
                {
                    this.Clients.Add(client.lmlTk, client);
                }
                catch (ArgumentException e)
                {
                    // Rollback
                    RemoveClient(client);
                    return false;
                }
            }

            return true;
        }

        public bool RemoveClient(ClientObject client)
        {
            bool success = false;

            lock (this.Rooms)
            {
                Room room = GetRoomByName(client.currentUrl);

                lock (room)
                {
                    if (room != null)
                    {
                        lock (this.Clients)
                        {
                            success = room.Clients.Remove(client) && this.Clients.Remove(client.lmlTk);
                            if (success)
                            {
                                if (room.ClientNumber == 0) this.Rooms.Remove(room);
                            }
                        }
                    }
                }
            }

            return success;
        }
    }

    /// <summary>
    /// Represent a group of client object that should all receive the same WebSOcket events
    /// </summary>
    public class Room
    {
        public string Name;

        [NonSerialized()]
        public List<ClientObject> Clients;

        public int ClientNumber
        { get { return this.Clients.Count; } }

        public Room(string name)
        {
            this.Name = name;
            this.Clients = new List<ClientObject>(50);
        }

        /// <summary>
        /// Sends a given application message to the websocket associated to every client in the room
        /// </summary>
        /// <param name="messageCode">The application message code t send</param>
        /// <returns>The number of messages sent</returns>
        public int Broadcast(WebSocketMessage.ApplicationMessageCode messageCode)
        {
            int messagesSent = 0;

            if (this.Clients != null && this.Clients.Count > 0)
            {
                lock (this.Clients)
                {
                    this.Clients.ForEach(client => client.SendApplicationMessage(messageCode));
                }
            }

            return messagesSent;
        }
    }
}
