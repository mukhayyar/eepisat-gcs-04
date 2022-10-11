using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCS_G03.Models
{
    public class ViewModel
    {
        public double AltitudeValue { get; set; }
        public double PayloadValue { get; set; }
        public double GpsValue { get; set; }

        public double RAccelValue { get; set; }
        public double PAccelValue { get; set; }
        public double YAccelValue { get; set; }

        public double AltitudeTempValue { get; set; }
        public double PayloadTempValue { get; set; }

        public double RGyroValue { get; set; }
        public double PGyroValue { get; set; }
        public double YGyroValue { get; set; }

        public double VoltValue { get; set; }

        public double RMagValue { get; set; }
        public double PMagValue { get; set; }
        public double YMagValue { get; set; }

        public double t { get; set; }

        public ViewModel()
        {

        }
    }
}
