# Simple Server

This is a server/client benchmark designed to test threadpool performance.

## Starting the Server

```
mono Server.exe 100 &
```

Starts a server that will accept 100 connections before shutting down.

## Starting the Clients

```
mono Client.exe
```

Starts a client that will connect to the server, exchange ten thousand
half-kilobyte request-response pairs, then send an EOT to terminate the
connection.
