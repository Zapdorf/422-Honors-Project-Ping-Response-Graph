using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using System;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;

namespace AvaloniaApplication1.ViewModels;

public class MainViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";

    //public static ISeries[] Series { get; set; } 
    //    = new ISeries[]
    //    {
    //        new LineSeries<int>
    //            {
    //                Values = new int[] { 4, 6, 5, 3, -3, -1, 2 }
    //            }
    //    };

    public static Axis[] XAxes { get; set; }
    = new Axis[]
    {
        //new Axis
        //{
        //    Name = "Ping Response Time",
        //    NamePaint = new SolidColorPaint(SKColors.White),
        //    NameTextSize = 15,

        //    LabelsPaint = new SolidColorPaint(SKColors.White),
        ////    //TextSize = 5,

        ////    //SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 2 }

        ////    //MinLimit = -0.5
        //},
        new DateTimeAxis(TimeSpan.FromSeconds(1), date => date.ToString("HH:mm:ss:fff"))
    };
    

    public Axis[] YAxes { get; set; }
    = new Axis[]
    {
        new Axis
        {
            Name = "Round Trip Time (ms)",
            NamePaint = new SolidColorPaint(SKColors.White),
            NameTextSize = 15,

            LabelsPaint = new SolidColorPaint(SKColors.White),
        }
    };



    private static ObservableCollection<DateTimePoint> _observableValues;
    public ObservableCollection<ISeries> Series { get; set; }

    public MainViewModel()
    {
        XAxes[0].Name = "Time Recorded";
        XAxes[0].NamePaint = new SolidColorPaint(SKColors.White);
        XAxes[0].NameTextSize = 15;
        XAxes[0].LabelsPaint = new SolidColorPaint(SKColors.White);


        // Use ObservableCollections to let the chart listen for changes (or any INotifyCollectionChanged). 
        _observableValues = new ObservableCollection<DateTimePoint>
        {
            // Use the ObservableValue or ObservablePoint types to let the chart listen for property changes 
            // or use any INotifyPropertyChanged implementation 
            //new ObservableValue(2)
        };

        Series = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = _observableValues,
                Fill = null
            }
        };

        // in the following sample notice that the type int does not implement INotifyPropertyChanged
        // and our Series.Values property is of type List<T>
        // List<T> does not implement INotifyCollectionChanged
        // this means the following series is not listening for changes.
        // Series.Add(new ColumnSeries<int> { Values = new List<int> { 2, 4, 6, 1, 7, -2 } }); 
    }

    public ISeries[] Series2 { get; set; } =
    {
        new LineSeries<DateTimePoint>
        {
            Values = new ObservableCollection<DateTimePoint>
            {
                
            }
        }
    };

    public static void AddItem(double val)
    {
        _observableValues.Add(new(DateTime.Now, val));
    }

    public static void ClearDataList(){
        _observableValues.Clear();
        ResetView();
    }

    public static void ResetView(){
        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;
    }

    public static void LoadDataPoint(DateTime time, double value)
    {
        _observableValues.Add(new(time, value));
    }

    public static List<Tuple<DateTime, double?>> SaveDataPoints(string pingTarget)
    {
        // text file 127_0_0_1.txt

        // return data

        List<Tuple<DateTime, double?>> result = new List<Tuple<DateTime, double?>>();

        foreach(var value in _observableValues){
            result.Add(Tuple.Create(value.DateTime, value.Value));
        }

        return result;
    }

}
