using Avalonia.Controls;
using MsBox.Avalonia;
using PALC.Updater.ViewModels;
using System.Threading.Tasks;

namespace PALC.Updater.Views;

public partial class MainV : Window
{
    public MainVM vm;
    public MainV()
    {
        InitializeComponent();

        vm = new();
        DataContext = vm;

        vm.LoadExistingReleasesFailed += OnLoadExistingReleasesFailed;
    }


    private Task OnLoadExistingReleasesFailed(object? sender, System.Exception e)
    {
        
    }

    public async Task OnLoaded()
    {
        Window display = new() { Content = "Loading downloaded versions..." };

        display.Show();
        IsEnabled = false;

        await vm.LoadExistingReleases();

        IsEnabled = true;
        display.Close();
    }
}
