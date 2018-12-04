namespace NarcityMedia.Net
{
    public partial class WebSocketServer
    {
        public event WebSocketServerEvent OnConnect;
        public event WebSocketServerEvent OnDisconnect;
        public event WebSocketServerEvent OnMessage;

        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs a);

    }

    /// <summary>
    /// 
    /// </summary>
    public class WebSocketServerEventArgs
    {
        
    }
}
