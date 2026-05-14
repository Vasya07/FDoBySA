using FDoBySA.Helpers;
using System;
using System.Windows;
using System.Windows.Threading;

namespace FDoBySA.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            ContentFrame.Navigate(new BookCatalogPage());
            NavigationHelper.MainFrame = ContentFrame;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUIBasedOnRole();

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (s, args) => CheckFrozenStatus();
            timer.Start();
        }

        private void UpdateUIBasedOnRole()
        {
            btnAdmin.Visibility = UserSession.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            btnAuthor.Visibility = UserSession.IsAuthor ? Visibility.Visible : Visibility.Collapsed;

            CheckFrozenStatus();
        }

        private void CheckFrozenStatus()
        {
            UserSession.RefreshUser();

            if (UserSession.IsFrozen)
            {
                txtFrozenReason.Text = UserSession.FrozenReason;
                FrozenWarning.Visibility = Visibility.Visible;
            }
            else
            {
                FrozenWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCatalog_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new BookCatalogPage());
        }

        private void BtnLists_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new ReadingListsPage());
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new AdminPage());
        }

        private void BtnAuthor_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new AuthorPage());
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new UserProfilePage());
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Logout();

            LoginWindow login = new LoginWindow();
            login.Show();

            Close();
        }
    }
}