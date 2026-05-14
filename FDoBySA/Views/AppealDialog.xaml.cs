using System;
using System.Windows;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class AppealDialog : Window
    {
        public AppealDialog()
        {
            InitializeComponent();
            txtFrozenReason.Text = UserSession.FrozenReason;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string reason = txtAppealReason.Text.Trim();

            if (string.IsNullOrEmpty(reason))
            {
                ShowStatus("Введите обоснование для разморозки");
                return;
            }

            if (reason.Length < 20)
            {
                ShowStatus("Обоснование должно содержать минимум 20 символов");
                return;
            }

            var appeal = new UnfreezeRequests
            {
                UserId = UserSession.CurrentUser.UserId,
                TargetType = "Account",
                TargetId = UserSession.CurrentUser.UserId,
                Reason = reason,
                RequestDate = DateTime.Now,
                IsProcessed = false
            };

            Core.Context.UnfreezeRequests.Add(appeal);
            Core.Context.SaveChanges();

            MessageBox.Show("Апелляция отправлена администратору", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowStatus(string message)
        {
            txtStatus.Text = message;
            txtStatus.Visibility = Visibility.Visible;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, args) =>
            {
                txtStatus.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
    }
}