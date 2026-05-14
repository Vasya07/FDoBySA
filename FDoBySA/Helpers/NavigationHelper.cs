using System.Windows;
using System.Windows.Controls;

namespace FDoBySA.Helpers
{
    public static class NavigationHelper
    {
        public static Frame MainFrame { get; set; }

        public static void NavigateTo(Page page)
        {
            if (MainFrame != null)
            {
                MainFrame.Navigate(page);
            }
            else
            {
                MessageBox.Show("Ошибка навигации", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void GoBack()
        {
            if (MainFrame != null && MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }
    }
}