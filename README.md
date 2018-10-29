# .NET Core Websocket server
This application's goal is to implement real time functionnalities for the users of the platforms powered by Lilium CMS. It does so by using a custom implementation of the WebSocket protocol and making efficient usage of threads.
## Architecture
This application is intended to run on the same computer as a Lilium CMS instance.
This application is very performance oriented and is designed to handle multiple thousands of concurrent socket connections hence it makes the most out of the features offered by the .NET Core environnement.

This application comprises:
 - An HTTP server that listens to a loopback endpoint for requests coming from a Lilium CMS instance that indicate WebSockets events that need to be dispatched.
 - A main thread that listens for incomming HTTP requests that come directly from the clients (proxied by a web server like Nginx or Apache) to negotiate and initialize WebSocket connections.
 - A thread safe 'Socket Manager' that holds references to WebSockets connections keyed both by endpoints and by session token.

 The application, in order to be performant is highly multi-threaded and makes use, among other, of the Threadpool class provided by the .NET Core environnement. THe HTTP server handles HTTP requests asynchronously : it has a 'main thread' that listens for incomming HTTP requests and dispatches a thread to handle each incomming requests.

## Useful Links

Some links : 
http://lucumr.pocoo.org/2012/9/24/websockets-101/
https://tools.ietf.org/html/rfc6455
https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers
https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool?view=netframework-4.7.2