using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskLock.ViewModels;

namespace DeskLock.Views;

public partial class MainWindow : Window
{
    private Button? _enterArmedTimeOption;

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
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusSelectedStatusOption));
    }

    private void FocusSelectedStatusOption()
    {
        var statusButtons = FindGridButtons(StatusOptions);
        var selectedButton = statusButtons.FirstOrDefault(button =>
            DataContext is MainViewModel viewModel &&
            button.DataContext == viewModel.SelectedStatus);

        if ((selectedButton ?? statusButtons.FirstOrDefault()) is { } button)
        {
            FocusManager.SetFocusedElement(this, button);
            Keyboard.Focus(button);
        }
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

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var focusedTimeButton = Keyboard.FocusedElement is Button focusedButton &&
                                TimeOptions is not null &&
                                TimeOptions.Items.Contains(focusedButton.DataContext)
            ? focusedButton
            : null;
        var focusedStatusButton = Keyboard.FocusedElement is Button statusButton &&
                                   StatusOptions is not null &&
                                   StatusOptions.Items.Contains(statusButton.DataContext)
            ? statusButton
            : null;

        if (e.Key == Key.Return)
        {
            if (e.IsRepeat)
            {
                e.Handled = true;
                return;
            }

            if (focusedTimeButton is not null)
            {
                if (ReferenceEquals(_enterArmedTimeOption, focusedTimeButton))
                {
                    ExecuteLockCommand();
                }
                else
                {
                    ExecuteButtonCommand(focusedTimeButton);
                    _enterArmedTimeOption = focusedTimeButton;
                }
            }
            else
            {
                _enterArmedTimeOption = null;
                if (focusedStatusButton is not null &&
                    DataContext is MainViewModel viewModel &&
                    !ReferenceEquals(focusedStatusButton.DataContext, viewModel.SelectedStatus))
                {
                    ExecuteButtonCommand(focusedStatusButton);
                    e.Handled = true;
                    return;
                }

                ExecuteLockCommand();
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Space)
        {
            _enterArmedTimeOption = null;
            if (focusedTimeButton is not null)
            {
                ExecuteButtonCommand(focusedTimeButton);
                e.Handled = true;
            }
            return;
        }

        if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down))
        {
            return;
        }

        var focusedTimeOption = focusedTimeButton is not null;
        if (focusedTimeOption && (e.Key is Key.Up or Key.Down))
        {
            return;
        }

        if (!TryGetGridNavigationTarget(out var targetButtons, out var columns, out var currentIndex))
        {
            return;
        }

        var currentRow = currentIndex / columns;
        var currentColumn = currentIndex % columns;
        var targetRow = currentRow;
        var targetColumn = currentColumn;

        switch (e.Key)
        {
            case Key.Left:
                targetColumn--;
                break;
            case Key.Right:
                targetColumn++;
                break;
            case Key.Up:
                targetRow--;
                break;
            case Key.Down:
                targetRow++;
                break;
        }

        var totalRows = (int)Math.Ceiling(targetButtons.Count / (double)columns);
        if (targetButtons.Count == 0)
        {
            return;
        }

        if (targetColumn < 0)
        {
            targetColumn = columns - 1;
        }
        else if (targetColumn >= columns)
        {
            targetColumn = 0;
        }

        if (targetRow < 0)
        {
            targetRow = totalRows - 1;
        }
        else if (targetRow >= totalRows)
        {
            targetRow = 0;
        }

        var targetIndex = targetRow * columns + targetColumn;
        if (targetIndex >= targetButtons.Count)
        {
            targetIndex = targetButtons.Count - 1;
        }

        var targetButton = targetButtons[targetIndex];
        targetButton.Focus();
        if (!focusedTimeOption &&
            targetButton.Command is not null &&
            targetButton.Command.CanExecute(targetButton.CommandParameter))
        {
            targetButton.Command.Execute(targetButton.CommandParameter);
        }

        e.Handled = true;
    }

    private void Window_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (ReferenceEquals(e.OldFocus, _enterArmedTimeOption))
        {
            _enterArmedTimeOption = null;
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e) =>
        _enterArmedTimeOption = null;

    private void ExecuteLockCommand()
    {
        if (DataContext is MainViewModel viewModel &&
            viewModel.LockCommand.CanExecute(null))
        {
            viewModel.LockCommand.Execute(null);
        }
    }

    private static void ExecuteButtonCommand(Button button)
    {
        if (button.Command is not null &&
            button.Command.CanExecute(button.CommandParameter))
        {
            button.Command.Execute(button.CommandParameter);
        }
    }

    private bool TryGetGridNavigationTarget(out List<Button> targetButtons, out int columns, out int currentIndex)
    {
        targetButtons = new List<Button>();
        columns = 0;
        currentIndex = -1;

        var focusedElement = Keyboard.FocusedElement;
        var focusedButton = focusedElement as Button;
        if (focusedButton is not null &&
            StatusOptions is not null &&
            StatusOptions.Items.Contains(focusedButton.DataContext))
        {
            targetButtons = FindGridButtons(StatusOptions);
            columns = 2;
        }
        else if (focusedButton is not null &&
                 TimeOptions is not null &&
                 TimeOptions.Items.Contains(focusedButton.DataContext))
        {
            targetButtons = FindGridButtons(TimeOptions);
            columns = 5;
        }
        else if (focusedElement is null || ReferenceEquals(focusedElement, this))
        {
            if (StatusOptions is null ||
                DataContext is not MainViewModel viewModel ||
                viewModel.SelectedStatus is null)
            {
                return false;
            }

            targetButtons = FindGridButtons(StatusOptions);
            columns = 2;
            currentIndex = StatusOptions.Items.IndexOf(viewModel.SelectedStatus);
            return currentIndex >= 0 && currentIndex < targetButtons.Count;
        }
        else
        {
            return false;
        }

        currentIndex = targetButtons.IndexOf(focusedButton);
        return currentIndex >= 0;
    }

    private static List<Button> FindGridButtons(ItemsControl itemsControl)
    {
        var buttons = new List<Button>();
        if (itemsControl is null)
        {
            return buttons;
        }

        var queue = new Queue<DependencyObject>();
        queue.Enqueue(itemsControl);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var childCount = VisualTreeHelper.GetChildrenCount(current);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                if (child is Button button)
                {
                    buttons.Add(button);
                }
                queue.Enqueue(child);
            }
        }

        return buttons
            .Where(button => button.Parent is not null)
            .ToList();
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
