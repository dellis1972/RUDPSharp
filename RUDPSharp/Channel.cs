using System;

namespace RUDPSharp
{
    public enum Channel : byte {
        // Just send the data and hope it gets there. Packets can arrive out of order
        None = 0x00,
        // Send the data and hope it gets there. If and old packet arrives disgard it.
        InOrder = 0x01,
        // Send the data and wait for an ACK packet. If one does not arrive resend. If an old packet arrives disgard it,
        Reliable = 0x02,
        // Send the data and wait for an ACK packet. Packets will be processed in sequence order.
        // if a packet arrives and a sequence is skipped it will be queued for later use.
        ReliableInOrder = Reliable | InOrder,
    }
}