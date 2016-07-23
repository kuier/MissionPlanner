using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GMap.NET;

namespace MissionPlanner.Models
{
    public struct WucanshuDataModel
    {
        public PointLatLng Position { get; set; }
        public float DoValue { get; set; }
        public float TurValue { get; set; }
        public float CtValue { get; set; }
        public float PhValue { get; set; }
        public float TempValue { get; set; }
    }
}
