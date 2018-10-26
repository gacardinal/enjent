using System;
using System.Text;
using System.Collections.Generic;
using NarcityMedia.Net;

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
        /// Adds a given client to a specified room instance.
        /// If the room can't be found, it will be created.
        /// </summary>
        /// <param name="client">The client object to insert</param>
        public void AddClient(ClientObject client)
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
                // this.Clients.Add(client.lmlTk, client);
                this.Clients.Add(RandomString(32, false), client);
            }
        }

        /// <summary>
        /// Here for testing purposes only
        /// </summary>
        private string RandomString(int size, bool lowerCase)
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
                            success = room.Clients.Remove(client) && this.Clients.Remove(client.currentUrl);
                            client.Dispose();
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

        public List<ClientObject> Clients;

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
