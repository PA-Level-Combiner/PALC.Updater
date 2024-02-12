using Avalonia.Controls;

using PALC.Updater.ViewModels;

namespace PALC.Updater.Views;

public partial class DownloaderV : Window
{
    public DownloaderVM vm;

    public DownloaderV(DownloaderVM vm)
    {
        InitializeComponent();

        this.vm = vm;
        DataContext = vm;
    }
    public DownloaderV() : this(new()) { }



}
