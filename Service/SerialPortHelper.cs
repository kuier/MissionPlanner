using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;
using System.Windows.Forms;

namespace GuardShipSystem.Service
{
    class SerialPortHelper
    {
        public static readonly SerialPortHelper SerialPortHelperInstance = new SerialPortHelper();
        private readonly SerialPort _serialPort = new SerialPort();
        private SerialPortHelper()
        {
            //COM口
            _serialPort.PortName = "COM15";
            //波特率
            _serialPort.BaudRate = 9600;
            //奇偶校验检查
            _serialPort.Parity = Parity.None;
            //数据位
            _serialPort.DataBits = 8;
            //停止位
            _serialPort.StopBits = StopBits.One;
            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("COM15发生错误," + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        public void SendGpsData(string data)
        {
            try
            {
                _serialPort.Write("$" + data + "*" + check(data));
            }
            catch (Exception exception)
            {
                MessageBox.Show("写入串口时出错" + exception.Message);
            }
        }
        public static string check(string stringToCalculateTheChecksumOver)
        {
            byte checksum = 0;
            foreach (byte b in stringToCalculateTheChecksumOver)
            {
                checksum ^= b;
            }
            //输出十六进制
//            Console.WriteLine(checksum.ToString("X2"));
            return checksum.ToString("X2");
        }
    }
}
