/*
The MIT License (MIT)

Copyright (c) 2013, David Suarez

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
using System.IO;
using System.Text;
using System.Xml;

namespace CejuNET
{
    public class CejuPacket
    {
        public const int PacketOverheadNumBytes = 7;

        public bool IsValid = false;
        //操作码，表示测量长度
        public byte OperateSymbel;
        //数据
        public byte[] Data;
        public float Distance;
        public UasMessage Message;

        // __ Deserialization _________________________________________________

        /*
         * Byte order:
         * 
         * 0  Packet start sign	
         * 1	 Payload length	 0 - 255
         * 2	 Packet sequence	 0 - 255
         * 3	 System ID	 1 - 255
         * 4	 Component ID	 0 - 255
         * 5	 Message ID	 0 - 255
         * 6 to (n+6)	 Data	 (0 - 255) bytes
         * (n+7) to (n+8)	 Checksum (high byte, low byte) for v0.9, lowbyte, highbyte for 1.0
         *
         */
        public static CejuPacket Deserialize(BinaryReader s, byte payloadLength)
        {
            CejuPacket result = new CejuPacket()
            {
                OperateSymbel = s.ReadByte(),
            };

            // Read the payload instead of deserializing so we can validate CRC.
            result.Data = s.ReadBytes(3);
            result.Distance = ByteConvertToFloat(result.Data);
            result.IsValid = Math.Abs(result.Distance - 0.0) > 0.5  ;
            result.DeserializeMessage();

            return result;
        }
        private void DeserializeMessage()
        {
//            UasMessage result = UasSummary.CreateFromId(MessageId);
            UasMessage result = new UasMessage();

            if (result == null) return;  // Unknown type

            using (MemoryStream ms = new MemoryStream(4))
            {
                using (BinaryReader br = GetBinaryReader(ms))
                {
                    result.DeserializeBody(br);
                }
            }

            Message = result;
            IsValid = true;
        }

        public static BinaryReader GetBinaryReader(Stream s)
        {
            return new BinaryReader(s, Encoding.ASCII);
        }



        // CRC code adapted from Mavlink C# generator (https://github.com/mavlink/mavlink)

        const UInt16 X25CrcSeed = 0xffff;

        public static UInt16 X25CrcAccumulate(byte b, UInt16 crc)
        {
            unchecked
            {
                byte ch = (byte)(b ^ (byte)(crc & 0x00ff));
                ch = (byte)(ch ^ (ch << 4));
                return (UInt16)((crc >> 8) ^ (ch << 8) ^ (ch << 3) ^ (ch >> 4));
            }
        }

        private static float ByteConvertToFloat(byte[] array)
        {
            string temp = "";
            foreach (var b in array)
            {
                temp += b.ToString("X2");
            }
            try
            {
                
                return Convert.ToSingle(temp)/1000;
            }
            catch (Exception)
            {
                
                return 0;
            }
        }
    }
}
