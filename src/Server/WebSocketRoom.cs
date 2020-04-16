using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NarcityMedia.Enjent.WebSocket;

namespace NarcityMedia.Enjent.Server
{
	public class WebSocketRoom : WebSocketRoom<WebSocketClient>
	{
        public WebSocketRoom(WebSocketServer<WebSocketClient> server) : base(server)
        {}

        public WebSocketRoom(WebSocketServer<WebSocketClient> server, string name) : base(server, name)
        {}

        public WebSocketRoom(WebSocketServer<WebSocketClient> server, IEnumerable<WebSocketClient> clients) : base(server, clients)
        {}
	}

    /// <summary>
    /// A logical way to group <see cref="WebSocketClient" /> objects together.
    /// This class implements the ICollection interface for convenience.
    /// </summary>
    /// <typeparam name="WebSocketClient">The client objects to hold.true Can be a WebSocketClient or any derived type.</typeparam>
    public class WebSocketRoom<TWebSocketClient> : ICollection<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public readonly Guid Id;

        /// <summary>
        /// Indicates the name of the current WebSocketRoom.
        /// Use this attribute to classify your rooms
        /// </summary>
        public string? Name;

        /// <summary>
        /// Gets the number of <see cref="TWebSocketClient" /> in the current room
        /// </summary>
        /// <value>The number of clients in the room</value>
        public int Count
        {
            get { return this.clients.Count; }
        }

		/// <summary>
		/// Reference to the <see cref="WebSocketServerCore{TWebSocketClient}" /> that 'holds' the current instance of
		/// <see cref="WebSocketRoom{TWebSocketClient}" />.
		/// </summary>
		public readonly WebSocketServerCore<TWebSocketClient> Server;

        public bool IsReadOnly
        {
            get { return false; }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<TWebSocketClient> Clients
        {
            get { return this.clients.AsReadOnly(); }
        }
        
        /// <summary>
        /// The inner collection on which the current Room interfaces
        /// </summary>
        private List<TWebSocketClient> clients;

        public WebSocketRoom(WebSocketServerCore<TWebSocketClient> server)
        {
            this.Id = Guid.NewGuid();
			this.Server = server;
            this.clients = new List<TWebSocketClient>(100);
        }

        public WebSocketRoom(WebSocketServerCore<TWebSocketClient> server, string name) : this(server)
        {
            this.Name = name;
        }

        public WebSocketRoom(WebSocketServerCore<TWebSocketClient> server, IEnumerable<TWebSocketClient> clients) : this(server)
        {
            this.clients.AddRange(clients);
        }

        /// <summary>
        /// Sends a message to all the clients that are member of the current room
        /// </summary>
        /// <param name="message">The message to broadcast</param>
        public void Broadcast(BinaryMessage message)
        {
            lock (this.clients) 
            {
                foreach(TWebSocketClient cli in this.clients)
                {
                    this.Server.Send(cli, message);
                }
            }
        }

        public void Broadcast(string message)
        {
            lock (this.clients) 
            {
                foreach(TWebSocketClient cli in this.clients)
                {
                    this.Server.Send(cli, message);
                }
            }
        }

        public TWebSocketClient this[int index]
        {
            get 
            { 
                return (TWebSocketClient) clients[index]; 
            }
            set { 
                lock (this.clients) 
                {
                    clients[index] = value; 
                }
            }
        }

        public bool Contains(TWebSocketClient client)
        {
            lock (this.clients) 
            {
                foreach (TWebSocketClient cli in this.clients)
                {
                    if (cli.Equals(client))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Contains(WebSocketClient client, EqualityComparer<WebSocketClient> comp)
        {
            lock (this.clients) 
            {
                foreach (WebSocketClient cli in this.clients)
                {
                    if (comp.Equals(cli, client))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Add(TWebSocketClient cli)
        {
            lock (this.clients) 
            {
                this.clients.Add(cli);
            }
        }

        public void Clear()
        {
            lock (this.clients) 
            {
                this.clients.Clear();
            }
        }

        public void CopyTo(TWebSocketClient[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("The array cannot be null.");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("The starting array index cannot be negative.");
            if (Count > array.Length - arrayIndex + 1)
                throw new ArgumentException("The destination array has fewer elements than the collection.");
            
            for (int i = 0; i < this.clients.Count; i++) {
                array[i + arrayIndex] = this.clients[i];
            }
        }

        public bool Remove(TWebSocketClient cli)
        {
            lock (this.clients) {
                for (int i = 0; i < this.clients.Count; i++)
                {
                    TWebSocketClient curCli = (TWebSocketClient) this.clients[i];

                    if (curCli == cli)
                    {
                        this.clients.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerator<TWebSocketClient> GetEnumerator()
        {
            return new WebSocketClientEnumerator<TWebSocketClient>(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new WebSocketClientEnumerator<TWebSocketClient>(this);
        }
    }

    public class WebSocketClientEnumerator<TWebSocketClient> : IEnumerator<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        private WebSocketRoom<TWebSocketClient> room;
        private int curIndex;
        private TWebSocketClient? curCli;

        public WebSocketClientEnumerator(WebSocketRoom<TWebSocketClient> room)
        {
            this.room = room;
            curIndex = -1;
            curCli = default(TWebSocketClient);
        }

        public bool MoveNext()
        {
            if (++curIndex >= room.Count)
            {
				this.curCli = null;
                return false;
            }
            else
            {
                curCli = room[curIndex];
            }

            return true;
        }

        public void Reset() { curIndex = -1; }

        void IDisposable.Dispose() { }

        public TWebSocketClient Current
        {
            get
			{
				if (this.curCli != null)
				{
					return this.curCli;
				}
				else
				{
					throw new InvalidOperationException("Cannot access the current property when the enumerator is exhausted");
				}
			}
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}
