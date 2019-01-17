using System;
using System.Collections;
using System.Collections.Generic;

namespace NarcityMedia.Net
{
    /// <summary>
    /// A logical way to group <<see cref="WebSocketClient" /> objects together.
    /// This class implements the ICollection interface for convenience.
    /// </summary>
    /// <typeparam name="WebSocketClient">The client objects to hold.true Can be a WebSocketClient or any derived type.</typeparam>
    public class WebSocketRoom : ICollection<WebSocketClient>
    {
        /// <summary>
        /// Gets the number of <see cref="WebSocketClient" /> in the current room
        /// </summary>
        /// <value>The number of clients in the room</value>
        public int Count
        {
            get { return this.clients.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
        
        /// <summary>
        /// The inner collection on which the current Room interfaces
        /// </summary>
        private List<WebSocketClient> clients;
        
        public WebSocketRoom()
        {
            this.clients = new List<WebSocketClient>(100);
        }

        public WebSocketRoom(IEnumerable<WebSocketClient> clients)
        {
            this.clients.AddRange(clients);
        }

        /// <summary>
        /// Sends a message to all the clients that are member of the current room
        /// </summary>
        /// <param name="message">The message to broadcast</param>
        public void Broadcast(WebSocketMessage message)
        {
            foreach(WebSocketClient cli in this.clients)
            {
                cli.Send(message);
            }
        }

        public WebSocketClient this[int index]
        {
            get { return (WebSocketClient) clients[index]; }
            set { clients[index] = value; }
        }

        public bool Contains(WebSocketClient client)
        {
            foreach (WebSocketClient cli in this.clients)
            {
                if (cli.Equals(client))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(WebSocketClient client, EqualityComparer<WebSocketClient> comp)
        {
            foreach (WebSocketClient cli in this.clients)
            {
                if (comp.Equals(cli, client))
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(WebSocketClient cli)
        {
            this.clients.Add(cli);
        }

        public void Clear()
        {
            this.clients.Clear();
        }

        public void CopyTo(WebSocketClient[] array, int arrayIndex)
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

        public bool Remove(WebSocketClient cli)
        {
            for (int i = 0; i < this.clients.Count; i++)
            {
                WebSocketClient curCli = (WebSocketClient) this.clients[i];

                if (curCli.Equals(cli))
                {
                    this.clients.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public IEnumerator<WebSocketClient> GetEnumerator()
        {
            return new WebSocketClientEnumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new WebSocketClientEnumerator(this);
        }
    }

    public class WebSocketClientEnumerator : IEnumerator<WebSocketClient>
    {
        private WebSocketRoom room;
        private int curIndex;
        private WebSocketClient curCli;


        public WebSocketClientEnumerator(WebSocketRoom room)
        {
            this.room = room;
            curIndex = -1;
            curCli = default(WebSocketClient);
        }

        public bool MoveNext()
        {
            //Avoids going beyond the end of the collection.
            if (++curIndex >= room.Count)
            {
                return false;
            }
            else
            {
                // Set current box to next item in collection.
                curCli = room[curIndex];
            }
            return true;
        }

        public void Reset() { curIndex = -1; }

        void IDisposable.Dispose() { }

        public WebSocketClient Current
        {
            get { return curCli; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}