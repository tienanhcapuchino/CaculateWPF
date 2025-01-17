using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace Caculate.Helpers
{
    public static class AppHelper
    {
        public static async Task ShowPopupMessage(string message, PopupMessageType popupMessageType, DialogHost dialogHost)
        {
            // Create a new StackPanel for the dialog content
            var dialogContent = new StackPanel();

            //create icon kind
            var iconKind = PackIconKind.Information;
            switch(popupMessageType)
            {
                case PopupMessageType.Info:
                    iconKind = PackIconKind.Information;
                    break;
                case PopupMessageType.Error:
                    iconKind = PackIconKind.Error;
                    break;
                case PopupMessageType.Warning:
                    iconKind = PackIconKind.Warning;
                    break;
            }

            // Create the icon and set the type
            var icon = new PackIcon { Kind = iconKind, Width = 32, Height = 32, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10) };
            
            // Create the message text block
            var textBlock = new TextBlock
            {
                Text = message,
                Margin = new Thickness(10),
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Add the icon and text to the StackPanel
            dialogContent.Children.Add(icon);
            dialogContent.Children.Add(textBlock);

            // Create a button to close the dialog
            var button = new Button
            {
                Content = "OK",
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            button.Click += (sender, e) => DialogHost.CloseDialogCommand.Execute(null, null);

            // Add the button to the dialog content
            dialogContent.Children.Add(button);
            // Show the dialog using DialogHost
            await DialogHost.Show(dialogContent, dialogHost);
        }
    }

    public enum PopupMessageType : byte
    {
        Info = 0,
        Error = 1,
        Warning = 2
    }
}
