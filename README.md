# sample-code
Sample code for those interested in an example of my coding style.

This is a sample of the code for a project I did a while ago. None of this code is proprietary or confidential since the project it was written for has been cancelled. The part that was licensed, TightVNC, has been omitted. Therefore, this code/solution will not compile in this state. This code is intended for read/review only.

# Highlights

The overall purpose of this code is to enable a person with a pair of USB sticks loaded with this code to easy connect two computers for the purpose of remote control. This is accomplished by: a client application on the USB sticks which either waits for connections or performs a connection; a server that handles authentication and state management; a web application for managing user account information; and a packet relay service which is only needed if it is not possible to negotiate a peer-to-peer connection between the clients due to firewalls.

This project is comprised of the following components. Notable features are listed beneath each component title.

## Client

Path: MiracleSticksClient

The client is primarily a mixed-mode C# and C++ application which calls into TightVNC to perform the actual remote desktop functionality. Note that the TightVNC code is not included due to licensing restrictions.

## Server

Path
   - MiracleSticksServer
   - MiracleSticks.API

The server component performs client authentication and attempts to establish a session. Initially, one client will connect and wait until another client connects so that it can perform a VNC connection to the first and see/control their desktop. 

Note that the server runs as a service or console application by way of an implementation of the facade pattern. This allows easy debugging of the code in console mode and deployment as a Windows service.

## Packet Relay Server

Path: MiracleSticks.PacketRelay

The packet relay server is used whenever a peer-to-peer connection cannot be established. It accepts inbound TCP connections, correlates them by session identifier, and relays the VNC traffic bidirectionally. The packetRelay is fully multithreaded and can proxy thousands of simulataneous connections.

## Web UI

Path
   - MiracleSticks.WebAdmin
   - MiracleSticks.Model

The web UI is written in MVC4. Note the use of the model-view-controller pattern and dynamic updates via jquery.