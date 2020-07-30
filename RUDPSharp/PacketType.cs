using System;

namespace RUDPSharp
{
    public enum PacketType : byte {
        UnconnectedMessage = 0x0,
        Ack = 0x01, // Acknowledge recept of a packet
        Ping = 0x02,// A Ping, respond with a Pong
        Pong = 0x03,// A response to a Ping, used to track packet delivery times
        Connect = 0x04,// A new client wants to connect
        Disconnect = 0x05,// A client wants to disconnect.
        Data = 0x06, // User Data
    }
}