using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using System;

namespace Classic.Demo.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public DataDisplayViewModel DataDisplay { get; }
    public ObservableCollection<PersonViewModel> People => DataDisplay.People;

    [ObservableProperty]
    private int selectedCategoryIndex;

    [ObservableProperty]
    private DateTime? calendarDate = DateTime.Today;

    [ObservableProperty]
    private DateTimeOffset? selectedDate = new DateTimeOffset(DateTime.Today);

    [ObservableProperty]
    private TimeSpan? selectedTime = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

    [ObservableProperty]
    private int clickCount;

    [ObservableProperty]
    private string spinnerText = "Newbie Avalonia";

    private int spinnerIndex;

    [RelayCommand]
    private void CountClick() => ClickCount++;

    [RelayCommand]
    private void Spin(SpinEventArgs args)
    {
        spinnerIndex = (spinnerIndex + (args.Direction == SpinDirection.Decrease ? 4 : 1)) % 5;
        SpinnerText = spinnerIndex switch
        {
            0 => "Newbie Avalonia",
            1 => "Moderate Avalonia",
            2 => "Advanced Avalonia",
            3 => "Avalonia Expert",
            _ => "Avalonia Nightmare"
        };
    }

    public MainViewModel(DataDisplayViewModel dataDisplay)
    {
        DataDisplay = dataDisplay;
    }
}
