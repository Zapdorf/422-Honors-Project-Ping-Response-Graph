using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaApplication1.ViewModels;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{

    public bool CurrentlyPinging = false;
    public string PingTarget = "";

    public Avalonia.Threading.DispatcherTimer PingTimer;

    public ICMP_Ping pringles;

    public List<double> recordedTimes = new List<double>();

    private bool HostClearToPing;

    public MainWindow()
    {
        InitializeComponent();

        InitializeIpList();

        PingTimer = new Avalonia.Threading.DispatcherTimer();

        pingButton.IsEnabled = false;

        loadFileButton.Click += OpenFileButton_Clicked;

        saveResultsButton.Click += SaveDataButtonClicked;
        loadResultsButton.Click += LoadDataButtonClicked;

        clearResultsEditButton.Click += ClearGraphDataFromEdit;
        clearAddressesEditButton.Click += ClearAddressesFromEdit;

        resetSelectionButton.Click += ResetGraphView;

        pringles = new ICMP_Ping();

        this.Width = 1100;
        this.Height = 620;

    }

    private void InitializeIpList(){
        //ipList.ItemsSource = new List<string>() { "www.google.com", "www.lego.com" };
        ipList.ItemsSource = new List<string>();
    }

    private void AddItemToIpList(string item){
        var newList = ipList.Items.Select(x => x).ToList();
        newList.Add(item);
        ipList.ItemsSource = newList;
    }

    private void SetTimer()
    {
        PingTimer = new Avalonia.Threading.DispatcherTimer();
        PingTimer.Interval = TimeSpan.FromSeconds(2);
        PingTimer.Tick += TimerPing;
        PingTimer.Start();
    }

    private void TimerPing(object? sender, EventArgs e)
    {
        if (!CurrentlyPinging) return;

        if(PingTarget != null && PingTarget != ""){
            pingResultsList.Text = PingSite(PingTarget) + "\n" + pingResultsList.Text;
        }
    }

    private string PingSite(string site)
    {
        
        if(!HostClearToPing){
            HostClearToPing = pringles.SetNewAddress(PingTarget);
            return $"Failed to resolve host";
        }

        var timeResult = pringles.SendPing();

        // Error handling logic
        switch(timeResult){
            case -1:
                return $"Failed to resolve host";
            case -2:
                return $"Error creating raw socket";
            case -3:
                return $"No ping response";
            default:
                recordedTimes.Add(timeResult);
                MainViewModel.AddItem(timeResult);
                return $"reply from {site} {timeResult}ms";
        }
    }
    
    public void ResetGraphView(object sender, RoutedEventArgs args)
    {
        MainViewModel.ResetView();
    }

    public void StartStopPingClicked(object sender, RoutedEventArgs args)
    {
        CurrentlyPinging = !CurrentlyPinging;
        UpdatePingState();
    }

    private void UpdatePingState()
    {

        if (CurrentlyPinging)
        {
            PingTarget = ipList.SelectedValue?.ToString() ?? "";

            if (PingTarget == "" || PingTarget == null) return;


            HostClearToPing = pringles.SetNewAddress(PingTarget);

            ResetGraphData();
            SetTimer();
        }
        else
        {
            PingTimer.Stop();
            if (ipList.Items.Count < 1) pingButton.IsEnabled = false;
        }

        pingButton.Content = CurrentlyPinging ? "Stop Pinging" : "Start Pinging Selected Address";
    }

    private void ResetGraphData()
    {
        pingResultsList.Text = "";
        textWebsiteSelected.Text = PingTarget;

        recordedTimes.Clear();
        MainViewModel.ClearDataList();
    }

    public void TimeSelectionChanged(object sender, TimePickerSelectedValueChangedEventArgs args)
    {
        // both of the selectors call this
        pingResultsList.Text += "time changed";
        //MainViewModel.SetDataRange(lowerBoundDateInput.SelectedTime, DateTime.Now);
    }

    public void NewIPSelected(object sender, SelectionChangedEventArgs args)
    {
        pingButton.IsEnabled = true;
    }

    public async void SaveDataButtonClicked(object sender, RoutedEventArgs args)
    {

        var defaultTitle = PingTarget;
        defaultTitle = defaultTitle.Replace('.', '_');

        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Text File",
            SuggestedFileName = $"{defaultTitle}.txt",
            FileTypeChoices = new[] { TextFile }

        });

        List<string> linesToWrite = new List<string>() { PingTarget };

        var dataPoints = MainViewModel.SaveDataPoints(PingTarget);
        foreach (var point in dataPoints) {
            linesToWrite.Add(point.Item1.ToString());
            linesToWrite.Add(point.Item2.ToString());
        } 


        if (file is not null)
        {
            // Open writing stream from the file.
            await using var stream = await file.OpenWriteAsync();
            using var streamWriter = new StreamWriter(stream);

            // Write some content to the file.
            foreach(var line in linesToWrite){
                await streamWriter.WriteLineAsync(line);
            }
            
        }
    }

    public async void LoadDataButtonClicked(object sender, RoutedEventArgs args)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text Data File",
            AllowMultiple = false,
            FileTypeFilter = new[] { TextFile }
        });

        if (files.Count >= 1)
        {
            // Open reading stream from the first file.
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();

            var stringList = fileContent.Split("\n");

            // stop current pinging
            CurrentlyPinging = false;
            UpdatePingState();

            // load in name
            PingTarget = stringList[0].Trim();

            // reset graph
            ResetGraphData();

            // feed in data points
            for (var i=1; i <stringList.Length; i+=2)
            {
                if (stringList[i] == "") continue;
                DateTime time = DateTime.Parse(stringList[i]);
                double rtt = double.Parse(stringList[i + 1]);

                MainViewModel.LoadDataPoint(time, rtt);
            }
        }
    }

    public void ClearGraphDataFromEdit(object sender, RoutedEventArgs args)
    {
        CurrentlyPinging = false;
        UpdatePingState();
        textWebsiteSelected.Text = "";
        ResetGraphData();
    }

    public void ClearAddressesFromEdit(object sender, RoutedEventArgs args) 
    {
        ipList.ItemsSource = new List<string>();

        if (!CurrentlyPinging){
            pingButton.IsEnabled = false;
        }
        
    }

    public void AddNewIp(object sender, RoutedEventArgs args)
    {
        if(newIpBox.Text != "" && newIpBox.Text != null){
            AddItemToIpList(newIpBox.Text);
            newIpBox.Text = "";
        }
    }

    public static FilePickerFileType Config { get; } = new("Configuration")
    {
        Patterns = new[] { "*.cfg" },
        AppleUniformTypeIdentifiers = new[] { "public.cfg" }
        // https://docs.avaloniaui.net/docs/concepts/services/storage-provider/file-picker-options
    };

    public static FilePickerFileType TextFile { get; } = new("Data Text File")
    {
        Patterns = new[] { "*.txt" },
        AppleUniformTypeIdentifiers = new[] { "public.txt" }
    };

    private async void OpenFileButton_Clicked(object sender, RoutedEventArgs args)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Configuration File",
            AllowMultiple = false,
            FileTypeFilter = new[] { Config }
        }); 

        if (files.Count >= 1)
        {
            // Open reading stream from the first file.
            await using var stream = await files[0].OpenReadAsync();
            using var streamReader = new StreamReader(stream);
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();

            var stringList = fileContent.Split("\n");
            foreach(var thing in stringList){
                if (thing[0] == '#') continue;
                AddItemToIpList(thing.Trim());
            }
        }
    }
}
