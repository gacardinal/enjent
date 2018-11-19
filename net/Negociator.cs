using System;
using System.Net;
using System.Net.Sockets;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer
    {
        
        public static void NegociateWebSocketConnection(Object s)
        {
            Socket handler = (Socket) s;
            ClientObject cli = new ClientObject((Socket) handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
            {
                cli.Greet();
                cli.StartListenAsync();
                if (!SocketManager.Instance.AddClient(cli))
                {
                    cli.SendControlFrame(new SocketControlFrame(SocketFrame.OPCodes.Close));
                    cli.Dispose();
                }
            } else {
                cli.Dispose();
            }
        }
    }
}