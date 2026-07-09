using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using App.ViewModels;

namespace App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AddHandler(
            InputElement.KeyDownEvent,
            OnPreviewKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        ModalListBox.DoubleTapped += OnModalListBoxDoubleTapped;

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.IsModalOpen) && vm.IsModalOpen)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ModalSearchBox.Focus();
                            ModalSearchBox.CaretIndex = ModalSearchBox.Text?.Length ?? 0;
                        }, DispatcherPriority.Background);
                    }
                };
            }
        };
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
        {
            if (!vm.IsModalOpen)
                vm.IsModalOpen = true;

            Dispatcher.UIThread.Post(() =>
            {
                ModalSearchBox.Focus();
                ModalSearchBox.CaretIndex = ModalSearchBox.Text?.Length ?? 0;
            }, DispatcherPriority.Background);

            e.Handled = true;
            return;
        }

        // Media keys
        if (e.Key == Key.MediaPlayPause)
        {
            _ = vm.Player.TogglePlayPauseCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.MediaNextTrack)
        {
            _ = vm.Player.NextCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.MediaPreviousTrack)
        {
            _ = vm.Player.PreviousCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        if (!vm.IsModalOpen)
            return;

        if (e.Key == Key.Escape)
        {
            vm.IsModalOpen = false;
            _ = vm.StartModalCooldown();
            e.Handled = true;
            return;
        }

        var focused = TopLevel.GetTopLevel(this)?
            .FocusManager?
            .GetFocusedElement();

        if (e.Key == Key.Down && ReferenceEquals(focused, ModalSearchBox))
        {
            if (vm.Search.IsCommandMode && vm.Search.CommandItems.Count > 0)
            {
                FocusCommandItem(0);
            }
            else if (vm.Search.ModalItems.Count > 0)
            {
                FocusModalItem(0);
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up &&
            (IsInside(focused, ModalListBox) || IsInside(focused, CommandListBox)) &&
            GetActiveListSelectedIndex() <= 0)
        {
            ModalSearchBox.Focus();
            ModalSearchBox.CaretIndex = ModalSearchBox.Text?.Length ?? 0;

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (vm.Search.IsCommandMode)
            {
                vm.Search.ExecuteCommandCommand.Execute(null);
            }
            else if (ReferenceEquals(focused, ModalSearchBox))
            {
                _ = vm.Search.SearchCommand.ExecuteAsync(null);
            }
            else if (vm.Search.ModalItems.Count > 0 && ModalListBox.SelectedIndex >= 0)
            {
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    vm.Search.AddSelectedToQueue();
                }
                else
                {
                    vm.Search.PlaySelectedNow();
                }
            }

            e.Handled = true;
        }
    }

    private void FocusModalItem(int index)
    {
        ModalListBox.SelectedIndex = index;
        ModalListBox.ScrollIntoView(ModalListBox.SelectedItem);

        Dispatcher.UIThread.Post(() =>
        {
            ModalListBox.UpdateLayout();

            if (ModalListBox.ContainerFromIndex(index) is ListBoxItem item)
                item.Focus();
            else
                ModalListBox.Focus();

        }, DispatcherPriority.Background);
    }

    private void FocusCommandItem(int index)
    {
        CommandListBox.SelectedIndex = index;
        CommandListBox.ScrollIntoView(CommandListBox.SelectedItem);

        Dispatcher.UIThread.Post(() =>
        {
            CommandListBox.UpdateLayout();

            if (CommandListBox.ContainerFromIndex(index) is ListBoxItem item)
                item.Focus();
            else
                CommandListBox.Focus();

        }, DispatcherPriority.Background);
    }

    private int GetActiveListSelectedIndex()
    {
        if (CommandListBox.IsVisible)
            return CommandListBox.SelectedIndex;
        return ModalListBox.SelectedIndex;
    }

    private void OnModalListBoxDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        if (vm.Search.ModalItems.Count > 0 && ModalListBox.SelectedIndex >= 0)
        {
            vm.Search.PlaySelectedNow();
        }
    }

    private static bool IsInside(object? focused, Control parent)
    {
        var current = focused as Control;

        while (current is not null)
        {
            if (ReferenceEquals(current, parent))
                return true;

            current = current.Parent as Control;
        }

        return false;
    }

    private void CloseAuthMode(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Search.IsAuthMode = false;
        }
    }
}
