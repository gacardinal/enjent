using System;
using System.Collections.Generic;
using System.Linq;

namespace NarcityMedia.Enjent.Server
{
    /// <summary>
    /// Implements a 'non-binary tree-like' structure to 
    /// </summary>
    internal class WebSocketRoomDirectory<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public DirectoryNode<WebSocketRoom<TWebSocketClient>> Root;

        public WebSocketRoomDirectory()
        {
            this.Root = new DirectoryNode<WebSocketRoom<TWebSocketClient>>("/", new WebSocketRoom<TWebSocketClient>());
            
        }

        public WebSocketRoom<TWebSocketClient> AddClientToRoom(TWebSocketClient cli, string[] segments)
        {
            DirectoryNode<WebSocketRoom<TWebSocketClient>> leafNode = Root;
            for (int i = 0; i < segments.Length; i++)
            {
                DirectoryNode<WebSocketRoom<TWebSocketClient>> child;
                if (leafNode.FindNode(segments[i], out child))
                {
                    leafNode = child;
                }
                else
                { // If node doesn't exist, create all the necessary nodes to represent the desired path
                    child = new DirectoryNode<WebSocketRoom<TWebSocketClient>>(segments[i], new WebSocketRoom<TWebSocketClient>());
                    for (int j = i; i < segments.Length; j++)
                    {
                        DirectoryNode<WebSocketRoom<TWebSocketClient>> pathNode = new DirectoryNode<WebSocketRoom<TWebSocketClient>>(
                            segments[j], new WebSocketRoom<TWebSocketClient>()
                        );

                        child.AddNode(pathNode);
                        if (j >= segments.Length)
                        {
                            leafNode = pathNode;
                        }
                    }

                    break;
                }
            }

            leafNode.Value.Add(cli);
            return leafNode.Value;
        }
    }

    internal class DirectoryNode<TValue>
    {
        string Identifier;

        public List<DirectoryNode<TValue>> Children;

        public TValue Value;

        public DirectoryNode(string identifier, TValue value)
        {
            this.Identifier = identifier;
            this.Children = new List<DirectoryNode<TValue>>();
            this.Value = value;
        }

        public void AddNode(string identifier, TValue value)
        {
            this.AddNode(new DirectoryNode<TValue>(identifier, value));
        }

        public void AddNode(DirectoryNode<TValue> node)
        {
            this.Children.Add(node);
        }

        public DirectoryNode<TValue>? RemoveNode(string identifier)
        {
            DirectoryNode<TValue>? node = this.Children.FirstOrDefault(n => n.Identifier == identifier);
            if (node != null)
            {
                this.Children.Remove(node);
            }

            return node;
        }

        public bool FindNode(string identifier, out DirectoryNode<TValue> dirNode)
        {
            dirNode = this.Children.FirstOrDefault(n => n.Identifier == identifier);
            return dirNode == null;
        }
    }
}
