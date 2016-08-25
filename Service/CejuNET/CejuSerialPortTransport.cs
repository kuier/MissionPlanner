/*
The MIT License (MIT)

Copyright (c) 2014, Håkon K. Olafsen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;

namespace CejuNET
{
    public class CejuSerialPortTransport : CejuGenericTransport
    {
        public string SerialPortName = "COM15";
        public int BaudRate = 9600;
        private ConcurrentQueue<byte[]> mReceiveQueue = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<UasMessage> mSendQueue = new ConcurrentQueue<UasMessage>();
        private AutoResetEvent mReceiveSignal = new AutoResetEvent(true);
        private AutoResetEvent mSendSignal = new AutoResetEvent(true);
        private CejuAsyncWalker _mCeju = new CejuAsyncWalker();
        private SerialPort mSerialPort;
        private bool mIsActive = true;


        public override void Initialize()
        {
        }

        public void InitializerSerialPort()
        {
            InitializeMavLink();
            InitializeSerialPort(SerialPortName);
        }

        public override void Dispose()
        {
            mIsActive = false;
            mSerialPort.DataReceived -= DataReceived;
            mSerialPort.Close();
            mReceiveSignal.Set();
            mSendSignal.Set();
        }

        private void InitializeMavLink()
        {
            _mCeju.PacketReceived += HandlePacketReceived;
        }

        private void InitializeSerialPort(string serialPortName)
        {
            mSerialPort = new SerialPort(serialPortName) { BaudRate = BaudRate };
            mSerialPort.Open();
            mSerialPort.DataReceived += DataReceived;

            // Start receive queue worker
            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessReceiveQueue), null);

            // Start send queue worker
            ThreadPool.QueueUserWorkItem(
                new WaitCallback(ProcessSendQueue));
        }


        // __ Receive _________________________________________________________
        
        
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var serialPort = (SerialPort)sender;
                var buffer = new byte[serialPort.BytesToRead];
                
                serialPort.Read(buffer, 0, serialPort.BytesToRead);
                mReceiveQueue.Enqueue(buffer);
                
                // Signal processReceive thread
                mReceiveSignal.Set();
            }
            catch (TimeoutException)
            {
                mIsActive = false;
            }
            catch (InvalidOperationException)
            {
                mIsActive = false;
            }
        }

        private void ProcessReceiveQueue(object state)
        {
            while (true)
            {
                byte[] buffer;

                if (mReceiveQueue.TryDequeue(out buffer))
                {
                    _mCeju.ProcessReceivedBytes(buffer, 0, buffer.Length);
                }
                else
                {
                    // Empty queue, sleep until signalled
                    mReceiveSignal.WaitOne();

                    if (!mIsActive) break;
                }
            }

            HandleReceptionEnded(this);
        }


        // __ Send ____________________________________________________________


        private void ProcessSendQueue(object state)
        {
            while (true)
            {
                UasMessage msg;

                if (mSendQueue.TryDequeue(out msg))
                {
                    SendMavlinkMessage(msg);
                }
                else
                {
                    // Queue is empty, sleep until signalled
                    mSendSignal.WaitOne();

                    if (!mIsActive) break;
                }
            }
        }

        private void SendMavlinkMessage(UasMessage msg)
        {
//            byte[] buffer = _mCeju.SerializeMessage(msg, MavlinkSystemId, MavlinkComponentId, true);

//            mSerialPort.Write(buffer, 0, buffer.Length);
        }


        // __ API _____________________________________________________________

        public bool SendStartCommand()
        {
            byte[] bytes= new byte[1]{0xb1};
            try
            {
                mSerialPort.Write(bytes, 0, 1);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SendStartCommand2()
        {
            byte[] bytes = new byte[1] { 0xb2 };
            try
            {
                mSerialPort.Write(bytes, 0, 1);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SendStopCommand()
        {
            byte[] bytes = new byte[1] { 0x00 };
            try
            {
                mSerialPort.Write(bytes, 0, 1);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void SendMessage(UasMessage msg)
        {
            mSendQueue.Enqueue(msg);

            // Signal send thread
            mSendSignal.Set();
        }
    }
}
