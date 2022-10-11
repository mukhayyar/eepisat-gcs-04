using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCS_G03.Models;

namespace GCS_G03.ViewModels.GyroGraph
{
    public class YGyroVM
    {
        public readonly ObservableCollection<ViewModel> DataPoints;
        public YGyroVM()
        {
            DataPoints = new ObservableCollection<ViewModel>()
            {

            };
        }

        public void AddDataPoint(double ekg, double time, int windowSize)
        {
            if (DataPoints.Count > windowSize)
            {
                DataPoints.RemoveAt(0);
            }
            DataPoints.Add(new ViewModel()
            {
                YGyroValue = ekg,
                t = time
            });
        }
    }
}
