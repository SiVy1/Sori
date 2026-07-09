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
            if (vm.ModalItems.Count > 0)
                FocusModalItem(0);

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up && IsInside(focused, ModalListBox) && ModalListBox.SelectedIndex <= 0)
        {
            ModalSearchBox.Focus();
            ModalSearchBox.CaretIndex = ModalSearchBox.Text?.Length ?? 0;

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (vm.IsCommandMode)
            {
                vm.ExecuteCommandCommand.Execute(null);
            }
            else if (ReferenceEquals(focused, ModalSearchBox))
            {
                _ = vm.SearchCommand.ExecuteAsync(null);
            }
            else if (vm.ModalItems.Count > 0 && ModalListBox.SelectedIndex >= 0)
            {
                var selected = vm.ModalItems[ModalListBox.SelectedIndex];
                vm.OpenHomeItemCommand.Execute(selected);
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
}
