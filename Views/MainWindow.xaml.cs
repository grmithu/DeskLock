using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeskLock.ViewModels;

namespace DeskLock.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>App theke bondho korar shomoy true kora hoy; nahole close korle shudhu hide hoy.</summary>
    public bool AllowClose { get; set; }

    public void ShowPopup()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Reset();
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ExactTimeDial_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Canvas dial && DataContext is MainViewModel viewModel)
        {
            var point = e.GetPosition(dial);
            viewModel.SelectClockDialPoint(point.X, point.Y);
            e.Handled = true;
        }
    }
}
