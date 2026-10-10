using System.Collections.ObjectModel;

namespace Classic.Demo.ViewModels;

public sealed class DataDisplayViewModel : ViewModelBase
{
    public ObservableCollection<PersonViewModel> People { get; } =
    [
        new PersonViewModel("Neil", "Armstrong"),
        new PersonViewModel("Buzz", "Lightyear") { Selected = true },
        new PersonViewModel("James", "Kirk")
    ];
}
