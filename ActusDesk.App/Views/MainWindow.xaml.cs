using System.Windows;
using ActusDesk.App.ViewModels;

namespace ActusDesk.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
