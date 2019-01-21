namespace NarcityMedia.Net
{
    interface INegotiationStrategy
    {
        WebSocketClient Negotiate();
    }

    sealed class DefaultNegotiationStrategy : INegotiationStrategy
    {
        public WebSocketClient Negotiate()
        {
            return new WebSocketClient();
        }
    }
}
