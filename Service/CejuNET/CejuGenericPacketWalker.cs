using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CejuNET
{
    public delegate void PacketReceivedDelegate(object sender, CejuPacket packet);


    public abstract class CejuGenericPacketWalker
    {
        public const byte PacketSignalByte = 0xFF;

        /// <summary>
        /// Event raised everytime a packet is received. This event is synchronous, 
        /// no further packet processing occurs until the event handler returns.
        /// </summary>
        public event PacketReceivedDelegate PacketReceived;

        /// <summary>
        /// Event raised everytime a stream of data fails a CRC check. 
        /// This event is synchronous, no further packet processing occurs until 
        /// the event handler returns.
        /// </summary>
        public event PacketReceivedDelegate PacketDiscarded;

        /// <summary>
        /// Processes a buffer of bytes. When a packet is complete, a PacketReceived 
        /// event is raised.
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void ProcessReceivedBytes(byte[] buffer, int start, int length);

        // __ Impl ____________________________________________________________


        protected void NotifyPacketReceived(CejuPacket packet)
        {
            if (packet == null || PacketReceived == null) return;

            PacketReceived(this, packet);
        }

        protected void NotifyPacketDiscarded(CejuPacket packet)
        {
            if (packet == null || PacketDiscarded == null) return;

            PacketDiscarded(this, packet);
        }
    }
}
