﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CejuNET;

namespace CejuNET
{
    public abstract class CejuGenericTransport: IDisposable
    {
        public byte MavlinkSystemId = 200;
        public byte MavlinkComponentId = 1;
        //public MavLinkState UavState = new MavLinkState();

        public event PacketReceivedDelegate OnPacketReceived;
        public event EventHandler OnReceptionEnded;

        public abstract void Initialize();
        public abstract void Dispose();
        public abstract void SendMessage(UasMessage msg);
        //public abstract void SendRawPacket(CejuPacket packet);


        // __ MavLink events __________________________________________________


        protected void HandlePacketReceived(object sender, CejuPacket e)
        {
            if (OnPacketReceived != null) OnPacketReceived(sender, e);
        }

        protected void HandleReceptionEnded(object sender)
        {
            if (OnReceptionEnded != null) OnReceptionEnded(sender, EventArgs.Empty);
        }
    }
}
