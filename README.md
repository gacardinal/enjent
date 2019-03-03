# Introduction
This repository is a class library that implements the WebSocketProtocol on the server side only. It it very performance focused and is able to handle around 8000 clients with ± 50 MB RAM and ± 1% CPU on a quad core machine in production.

# Getting Started
Follow these steps to start using this library
1. If you don't already have a dotnet project, start by creating a new one with `dotnet new console` which will create a new blank Console application.
2. Download and add a reference ot the NuGet package by running `dotnet add reference <path/to/nuget/file>. 
*This step will become simpler once the classlib is live on nuget.org*
3. Now you will have to 'use' the `NarcityMedia.Enjent` namespace which holds the `WebSocketServer` class, among others.
4. Next, create, configure and start a `WebSocketServer`. Here's an example of a typical use of the `WebSocketServer` class.
```csharp
using System;
using System.Net;
using NarcityMedia.Enjent;

namespace UseSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instantiate a WebSocketServer - it won't be running at this point
            WebSocketServer Wss = new WebSocketServer();

            // Add event handlers
            Wss.OnConnect += HandleConnect;
            Wss.OnMessage += HandleMessage;
            Wss.OnDisconnect += HandleDisconnect;

            // Start the server
            try
            {
                Wss.Start(new IPEndPoint(IPAddress.Loopback, 13003));
            }
            catch (WebSocketServerException e)
            {
                Console.WriteLine("An error occured when starting the WebSocket server - " + e.Message);
            }
        }

        public static void HandleConnect(object sender, WebSocketServerEventArgs args)
        {
            // Execute logic on socket connection
            Console.WriteLine("Got a new socket ID: " + args.Cli.Id);
        }
        
        public static void HandleMessage(object sender, WebSocketServerEventArgs args)
        {
            // Execute logic on socket message
            Console.WriteLine(String.Format("User {0} says : {1}", args.Cli.Id, args.DataFrame.Plaintext));
        }
        
        public static void HandleDisconnect(object sender, WebSocketServerEventArgs args)
        {
            // Execute logic on socket disconnect
            Console.WriteLine(String.Format("User {0} disconnected", args.Cli.Id));
        }
    }
}
```
Your output should look something like this

![Example Output](./assets/example_output.png)

That's it! You're good to go!

# Deploy
At this point, we do not consider this classlib to be 'production ready', a lot of testing and improvements remain to be done.

However, if you were to deploy this to the public, you need to use the `wss` uri scheme (which is a TLS layer over the WebSocket protocol) to connect to the server from a cient. Most browsers will disallow connecting via plain `ws` to a server if you're not connecting to `localhost` and as a responsible developer, you should `not` allow application data to be transfer unciphered anyway.

In order to enable clients to connect via `wss`, you will have to deploy your WebSocket server behind a server that will act as a reverse proxy to handle ciphered traffic such as NGINX or Apache. This classlib is not intended to be deployed directly to the Internet.

## NGINX Configuration Example
This is an example of how you could configure an NGINX server to handle ciphered traffic and reverse proxy requests coming from the WebSocket client to your WebSocket server.

```
upstream dotnet_websocket_proxy {
    server 127.0.0.1:13003;
    keepalive 64; 
}

server {
    server_name websocket.your_domain.com;

    root /some/public/directory; # Could be /var/www/yoursite

    listen 443 ssl; 
	
	# If you use Certbot to generate HTTPS certs, it will likely add the following part automatically
    ssl_certificate /etc/letsencrypt/live/websocket.your_domain.com/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/websocket.your_domain.com/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot
	# End of Certbot section

    location / { 
        add_header "Access-Control-Allow-Origin" "https://your_domain.com";
        add_header "Access-Control-Allow-Methods" "GET, POST, OPTIONS, HEAD";
        add_header "Access-Control-Allow-Headers" "Authorization, Origin, X-Requested-With, Content-Type, Accept, Sec-WebSocket-Version, Sec-We

        proxy_set_header "Access-Control-Allow-Origin" "https://your_domain.com";
        proxy_set_header "Access-Control-Allow-Methods" "GET, POST, OPTIONS, HEAD";
        proxy_set_header "Access-Control-Allow-Headers" "Authorization, Origin, X-Requested-With, Content-Type, Accept, Sec-WebSocket-Version, 

		# If you receive real ip from Cloudflare, this will map it to "X-Real-Ip" header
        proxy_set_header X-Real-Ip $http_cf_connecting_ip;
        proxy_set_header Host $http_host;
        proxy_set_header Host $host;

        proxy_buffers 8 32k;
        proxy_buffer_size 64k;

        proxy_pass http://dotnet_websocket_proxy;
    }
}
```

# How it Works Under the Hood
If you are interested in exploring the 'low-level' workings of the WebSocket protocol itself, there really is no better way than to start by reading the [WebSocket specification (RFC 6455)](https://tools.ietf.org/html/rfc6455 "RFC 6455").

Here's an oversimplified overview of the workings of this classlib:
 - When the `WebSocketServer` class is instantiated, it creates a new `Socket` object. This socket will be responsible for listening for incoming HTTP requests.
 - When the `WebSocketServer.Start()` method is called, the `WebSocketServer` class tries to bind it's HTTP listener `Socket` on the specified `Endpoint` and listens for incoming connections using a *dedicated Thread* as to not block the execution of your code that called the `WebSocketServer.Start()` method.
 - When an HTTP request is accepted by the `WebSocketServer`, it uses a `ThreadPool` thread to negotiates the WebSocket connection as per the RFC 6455 specifications and returns a `101 Upgrade` HTTP status code to the client if all went well. This way, it can go back to listening for HTTP requests immediately.
 - In the case where the negotiation was successful, the `WebSocketServer` object will invoke its `OnConnect` event, providing your code with a `WebSocketClient` object that represents the newly acquired connection. \
*Note:* If you wish to use a custom object instead of the default `WebSocketClient`, you can use the generic `WebSocketServer<TWebSocketClient>` class, where `TWebSocketClient` is a type that is derived from `WebSocketClient`. The generic `WebSocketServer<TWebSocketClient>` class requires that you provide a `ClientInitializationStrategy`, which is a callback that is invoked right before the `OnConnect` event and that is used to initialize an instance of your custom `TWebSocketClient` type.
 - When the `WebSocketServer` receives a WebSocket `Frame` from a client, the `OnMessage` event event is invoked if the received frame was a 'data frame', that is, a frame that is *not* a 'control frame'. Reffer to the [WebSocket specification (RFC 6455)](https://tools.ietf.org/html/rfc6455 "RFC 6455") specification for the definition of a 'data frame' and a 'control frame'.
 - The `WebSocketServer`  invokes the `OnDisconnect` event when a client closes the connection to the server.

## Contributors ##
 - [Gabriel Cardinal](https://github.com/Gaboik)
 - [Erik Desjardins](https://github.com/rykdesjardins)
 - [Narcity Media](https://github.com/narcitymedia)

## License ##
Mozilla Public License Version 2.0

## About the license
_TL;DR : You can use the library to make money as long as it remains open source. The typical use case involves no additional work._

Both individuals and businesses are allowed to use Enjent. 
You are allowed to host a copy of Enjent, modify it, redistribute it, create something different with it. 

One important thing to note : you **must** disclose the source code, copyright, and must document any modification done. A copy of the license must be available. However, this does not mean you need to credit Narcity Media on your website or anything of the sort. Simply make the source code available and highlight the modifications if any. 

That being said, you can still use Enjent for almost any purposes. 

Like most open source licenses, Narcity Media is not liable if anything happens with your server, data, etc. Enjent does not come with a warranty. 

The previous information is not legal advice, but should give you a good idea.

Mozilla Public License Version 2.0 is a simple, permissive license with conditions easy to respect. There have a [great FAQ here](https://www.mozilla.org/en-US/MPL/2.0/FAQ/).

## Copyright ##
© Narcity Media, 2019
