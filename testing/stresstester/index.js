var WebSocketClient = require('websocket').client;

let sockets = [];

process.on('SIGINT', function() {
    console.log("Caught interrupt signal");

    sockets.forEach(client => { client.d });

    process.exit();
});

setInterval(() => {
    var client = new WebSocketClient();
     
    client.on('connectFailed', function(error) {
        console.log('Connect Error: ' + error.toString());
    });

    client.on('connect', function(connection) {
        console.log('WebSocket Client Connected');
        connection.on('error', function(error) {
            console.log("Connection Error: " + error.toString());
        });
        connection.on('close', function() {
            console.log('echo-protocol Connection Closed');
        });
        connection.on('message', function(message) {
            if (message.type === 'utf8') {
                console.log("Received: '" + message.utf8Data + "'");
            }
        });
    });

    client.connect('ws://localhost:13003?www.someurl.com/');
    sockets.push(client);

    console.log("COnnections number: " + sockets.length);
}, 20);
