using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using SkiaSharp;
using LiveChartsCore.Defaults;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System;
using LiveChartsCore.Kernel.Sketches;
using System.Windows.Markup;

namespace WPFGraph
{
    public partial class ViewModel : ObservableObject
    {
        public double altitude = 0.0;
        private readonly ObservableCollection<ObservableValue> _observableValues;
        public ViewModel()
        {
            _observableValues = new ObservableCollection<ObservableValue> { };
            Series = new ObservableCollection<ISeries>
            {
                new LineSeries<ObservableValue>
                {
                    Values = _observableValues,
                    Fill = null
                }
            };
        }
        public ObservableCollection<ISeries> Series { get; set; }

        [RelayCommand]
        public void AddItem()
        {
            _observableValues.Add(new(altitude));  
            System.Diagnostics.Debug.WriteLine("Check here "+altitude);
            System.Diagnostics.Debug.WriteLine("Clicked here");
        }
    }

    
    }
