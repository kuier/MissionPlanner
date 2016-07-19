using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MissionPlanner.Service
{
    public class WaterColService
    {
        #region 单例
        public static readonly WaterColService WaterColServiceInstance = new WaterColService();
        private WaterColService()
        {

        } 
        #endregion

    }
}
