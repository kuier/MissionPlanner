using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MissionPlanner.Annotations;
using MissionPlanner.Attributes;

namespace MissionPlanner.Models
{
    public class WucanshuState:INotifyPropertyChanged
    {
        private float _doValue;

        private float _turValue;
        private float _ctValue;
        private float _phValue;
        private float _tempValue;
        [DisplayText("溶解氧(mg/L)")]
        public float DoValue
        {
            get { return _doValue; }
            set
            {
                _doValue = value;
                OnPropertyChanged("DoValue");
            }
        }

        [DisplayText("浊度(NTU)")]
        public float TurValue
        {
            get { return _turValue; }
            set
            {
                if (value.Equals(_turValue)) return;
                _turValue = value;
                OnPropertyChanged("TurValue");
            }
        }

        [DisplayText("电导率(ms/cm)")]
        public float CtValue
        {
            get { return _ctValue; }
            set
            {
                if (value.Equals(_ctValue)) return;
                _ctValue = value;
                OnPropertyChanged("CtValue");
            }
        }

        [DisplayText("PH")]
        public float PHValue
        {
            get { return _phValue; }
            set
            {
                if (value.Equals(_phValue)) return;
                _phValue = value;
                OnPropertyChanged("PHValue");
            }
        }

        [DisplayText("温度(℃)")]
        public float TempValue
        {
            get { return _tempValue; }
            set
            {
                if (value.Equals(_tempValue)) return;
                _tempValue = value;
                OnPropertyChanged("TempValue");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
