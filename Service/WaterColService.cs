using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using GuardShipSystem.Model;

namespace MissionPlanner.Service
{
    public class WaterColService
    {
        #region 单例

        private Modbus waterColModbus;
        public static readonly WaterColService WaterColServiceInstance = new WaterColService();
        private WaterColService()
        {
            waterColModbus = new Modbus();
            if (waterColModbus.Open("COM10", 9600, 8, Parity.None, StopBits.One))
            {

            }
            else
            {
                waterColModbus.Close();
                //                Application.Current.Shutdown();
            }
        } 
        #endregion
    }
}
