using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuardShipSystem.Model
{
    public class Modbus
    {
        private SerialPort sp = new SerialPort();
        public string modbusStatus;

        #region Constructor / Deconstructor,Modbus构造函数
        public Modbus()
        {
        }
        ~Modbus()
        {
        }
        #endregion

        #region Open / Close Procedures。打开和关闭端口
        public bool Open(string portName, int baudRate, int databits, Parity parity, StopBits stopBits)
        {
            //Ensure port isn't already opened:
            if (!sp.IsOpen)
            {
                //Assign desired settings to the serial port:
                sp.PortName = portName;
                sp.BaudRate = baudRate;
                sp.DataBits = databits;
                sp.Parity = parity;
                sp.StopBits = stopBits;
                //These timeouts are default and cannot be editted through the class at this point:
                sp.ReadTimeout = 5000;
                sp.WriteTimeout = 1000;

                try
                {
                    sp.Open();
                }
                catch (Exception err)
                {
                    modbusStatus = "Error opening " + portName + ": " + err.Message;
                    return false;
                }
                modbusStatus = portName + " opened successfully";
                return true;
            }
            else
            {
                modbusStatus = portName + " already opened";
                return false;
            }
        }
        public bool Close()
        {
            //Ensure port is opened before attempting to close:
            if (sp.IsOpen)
            {
                try
                {
                    sp.Close();
                }
                catch (Exception err)
                {
                    modbusStatus = "Error closing " + sp.PortName + ": " + err.Message;
                    return false;
                }
                modbusStatus = sp.PortName + " closed successfully";
                return true;
            }
            else
            {
                modbusStatus = sp.PortName + " is not open";
                return false;
            }
        }
        #endregion

        #region CRC Computation，CRC校验
        private void GetCRC(byte[] message, ref byte[] CRC)
        {
            //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
            //return the CRC values:

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }
        #endregion

        #region Build Message，构造一个消息
        private void BuildMessage(byte address, byte type, ushort start, ushort registers, ref byte[] message)
        {
            //Array to receive CRC bytes:比如 实际的 01 03 26 01 00 02 9E 83
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = type;
            message[2] = (byte)(start >> 8);
            message[3] = (byte)start;
            message[4] = (byte)(registers >> 8);
            message[5] = (byte)registers;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }
        #endregion

        #region Check Response，检验这个消息
        private bool CheckResponse(byte[] response)
        {
            //Perform a basic CRC check:
            byte[] CRC = new byte[2];
            GetCRC(response, ref CRC);
            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                return true;
            else
                return false;
        }
        #endregion

        #region Get Response，取得响应
        private void GetResponse(ref byte[] response)
        {
            //There is a bug in .Net 2.0 DataReceived Event that prevents people from using this
            //event as an interrupt to handle data (it doesn't fire all of the time).  Therefore
            //we have to use the ReadByte command for a fixed length as it's been shown to be reliable.
            for (int i = 0; i < response.Length; i++)
            {
                response[i] = (byte)(sp.ReadByte());
            }
        }
        #endregion

        #region Function 16 - Write Multiple Registers，写多个寄存器
        public bool SendFc16(byte address, ushort start, ushort registers, short[] values)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Message is 1 addr + 1 fcn + 2 start + 2 reg + 1 count + 2 * reg vals + 2 CRC
                byte[] message = new byte[9 + 2 * registers];
                //Function 16 response is fixed at 8 bytes
                byte[] response = new byte[8];

                //Add bytecount to message:
                message[6] = (byte)(registers * 2);
                //Put write values into message prior to sending:
                for (int i = 0; i < registers; i++)
                {
                    message[7 + 2 * i] = (byte)(values[i] >> 8);
                    message[8 + 2 * i] = (byte)(values[i]);
                }
                //Build outgoing message:
                BuildMessage(address, (byte)03, start, registers, ref message);

                //Send Modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in write event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    modbusStatus = "Write successful";
                    return true;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

        #region Build Message，构造一个五参数读取消息
        private void BuildMessage(byte address, byte type, byte thirdByte, byte fourthByte, ushort registers, ref byte[] message)
        {
            //Array to receive CRC bytes:比如 实际的 01 03 26 01 00 02 9E 83
            //通信状态查询： 10 30 01 30 05 6F
            //取水命令 ：10 10 03 01 01 f4 92 DB
            //涮桶命令：10 01 03 01 02 78 6F 8D
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = type;
            //            message[2] = (byte)(start >> 8);
            message[2] = thirdByte;
            message[3] = fourthByte;
            message[4] = (byte)(registers >> 8);
            message[5] = (byte)registers;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }
        #endregion

        #region Function 3 - Read Registers，读寄存器
        public bool SendFc3(byte address, byte start, ushort registers, ref short[] values)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];
                //Function 3 response buffer:
                byte[] response = new byte[5 + 2 * registers];
                //Build outgoing modbus message:
                BuildMessage(address, 0x03,0x26, start, registers, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    //Return requested register values:
                    for (int i = 0; i < (response.Length - 5) / 2; i++)
                    {
                        values[i] = response[2 * i + 3];
                        values[i] <<= 8;
                        values[i] += response[2 * i + 4];
                    }
                    modbusStatus = "Read successful";
                    return true;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }

        }
        #endregion

        #region SendTongXinMessage，发送通信状态查询命令
        /// <summary>
        /// 发送通信状态查询命令
        /// </summary>
        /// <param name="address">0x10</param>
        /// <param name="value">0x0130</param>
        /// <param name="registers">要读的寄存器个数</param>
        /// <param name="values">返回的值</param>
        /// <returns></returns>
        public bool SendTongXinMessage()
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[6];
                //Function 3 response buffer:
                byte[] response = new byte[6];
                //Build outgoing modbus message:
                //BuildMessage(address, 0x30, start, registers, ref message);
                BuildTongXinOrChaXunMessage(0x10, 0x30, 0x0130, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    //                    //Return requested register stateBytes:
                    //                    for (int i = 0; i < (response.Length - 4) / 2; i++)
                    //                    {
                    //                        stateBytes[i] = response[2 * i + 3];
                    //                    }
                    //values = response[3];
                    if (response[3] == 5)
                    {
                        modbusStatus = "通信成功";
                        return true;
                    }
                    modbusStatus = "read false，返回值为" + response[3];
                    return false;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }

        }
        /// <summary>
        /// 构造通信或者查询命令
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="message"></param>
        private void BuildTongXinOrChaXunMessage(byte address, byte type, ushort value, ref byte[] message)
        {
            //Array to receive CRC bytes:比如 实际的 01 03 26 01 00 02 9E 83
            //通信状态查询： 10 30 01 30 05 6F
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = type;
            message[2] = (byte)(value >> 8);
            message[3] = (byte)value;
            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }

        #endregion

        #region SendUpOrDownMessage，发送上升或下降查询命令
        /// <summary>
        /// 发送上升或下降命令 上升：0x0103 下降 0x0107
        /// </summary>
        /// <param name="fuc">上升：0x03 下降 0x07</param>
        /// <param name="cmd">上升：0x0103 下降 0x0107</param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool SendUpOrDownMessage(byte fuc, ushort cmd, ref short values)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[6];
                //Function 3 response buffer:
                byte[] response = new byte[6];
                //Build outgoing modbus message:
                //BuildMessage(address, 0x30, start, registers, ref message);
                BuildTongXinOrChaXunMessage(0x10, fuc, cmd, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    //                    //Return requested register stateBytes:
                    //                    for (int i = 0; i < (response.Length - 4) / 2; i++)
                    //                    {
                    //                        stateBytes[i] = response[2 * i + 3];
                    //                    }
                    values = response[3];
                    if (values == 5)
                    {
                        modbusStatus = "命令接收成功";
                        return true;
                    }
                    modbusStatus = "read false，返回值为" + values;
                    return false;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }

        }

        #endregion

        #region SendQuShuiMessage 发送取水命令
        /// <summary>
        /// 取水命令 10 10 03 01 01 f4 92 DB ， 涮桶命令 10 01 03 01 01 60 xx xx
        /// </summary>
        /// <param name="samPlingNum">桶号</param>
        /// <param name="value2">4位的采样量</param>
        /// <param name="values">返回值</param>
        /// <returns></returns>
        public bool SendQuShuiMessage(byte samPlingNum, ushort value2, ref short values)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];
                //Function 3 response buffer:
                byte[] response = new byte[6];
                BuildMessage(0x10, 0x10, 0x03, samPlingNum, value2, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    values = response[3];
                    if (values == 5)
                    {
                        modbusStatus = "接收成功";
                        return true;
                    }
                    modbusStatus = "read false，返回值为" + values;
                    return false;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

        #region Build Message，构造一个五参数读取消息
        private void BuildShuanTongMessage(byte address, byte type, byte thirdByte, byte samplingNum, byte washCount, byte washTime, ref byte[] message)
        {
            //Array to receive CRC bytes:比如 实际的 01 03 26 01 00 02 9E 83
            //通信状态查询： 10 30 01 30 05 6F
            //取水命令 ：10 10 03 01 01 f4 92 DB
            //涮桶命令：10 01 03 01 02 78 6F 8D
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = type;
            //            message[2] = (byte)(start >> 8);
            message[2] = thirdByte;
            message[3] = samplingNum;
            message[4] = washCount;
            message[5] = washTime;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }
        #endregion

        #region SendShuanTongMessage 发送取水和涮桶命令
        /// <summary>
        /// 取水命令 10 10 03 01 01 f4 92 DB ， 涮桶命令 10 01 03 01 01 60 xx xx
        /// </summary>
        /// <param name="samPlingNum">桶号</param>
        /// <param name="value2">4位的采样量或涮桶次数和涮桶时间</param>
        /// <param name="values">返回值</param>
        /// <returns></returns>
        public bool SendShuanTongMessage(byte samPlingNum, byte washCount, byte washTime, ref short values)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];
                //Function 3 response buffer:
                byte[] response = new byte[6];
                BuildShuanTongMessage(0x10, 0x01, 0x03, samPlingNum, washCount, washTime, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    values = response[3];
                    if (values == 5)
                    {
                        modbusStatus = "接收成功";
                        return true;
                    }
                    modbusStatus = "read false，返回值为" + values;
                    return false;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

        #region SendQuShuiMessage 发送取水查询命令
        /// <summary>
        /// 发送取水查询命令
        /// </summary>
        /// <param name="stateBytes"></param>
        /// <returns></returns>
        public bool SendQuShuiChaXunMessage(ref byte[] stateBytes)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                byte[] message = new byte[6];
                //Function 3 response buffer:
                byte[] response = new byte[9];
                BuildTongXinOrChaXunMessage(0x10, 0x30, 0x0110, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    Array.Copy(response, 3, stateBytes, 0, 4);
                    modbusStatus = "取水查询命令接收成功";
                    return true;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

        #region SendShuanTongChaXunMessage 发送涮桶查询命令
        public bool SendShuanTongChaXunMessage(ref byte[] values)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                //Function 3 request is always 8 bytes:
                byte[] message = new byte[6];
                //Function 3 response buffer:
                byte[] response = new byte[8];
                BuildTongXinOrChaXunMessage(0x10, 0x30, 0x0101, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    Array.Copy(response, 3, values, 0, 3);
                    modbusStatus = "涮桶信息查询接受成功";
                    return true;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

        #region SendUpDownChaXunMessage 发送上升和下降查询命令
        /// <summary>
        /// 发送上升下降查询命令
        /// </summary>
        /// <param name="cmd">上升：0x0103 ,下降： 0x0107</param>
        /// <param name="stateBytes"></param>
        /// <returns></returns>
        public bool SendUpDownChaXunMessage(ushort cmd, ref byte stateByte)
        {
            //Ensure port is open:
            if (sp.IsOpen)
            {
                //Clear in/out buffers:
                sp.DiscardOutBuffer();
                sp.DiscardInBuffer();
                byte[] message = new byte[6];
                //Function 3 response buffer:
                byte[] response = new byte[6];
                BuildTongXinOrChaXunMessage(0x10, 0x30, cmd, ref message);
                //Send modbus message to Serial Port:
                try
                {
                    sp.Write(message, 0, message.Length);
                    GetResponse(ref response);
                }
                catch (Exception err)
                {
                    modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }
                //Evaluate message:
                if (CheckResponse(response))
                {
                    stateByte = response[3];
                    modbusStatus = "上升下降查询命令接收成功";
                    return true;
                }
                else
                {
                    modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                modbusStatus = "Serial port not open";
                return false;
            }
        }
        #endregion

    }
}
