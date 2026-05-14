using System;
using System.Windows;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class ComplaintDialog : Window
    {
        private string _targetType;
        private int _targetId;

        public ComplaintDialog(string targetType, int targetId)
        {
            InitializeComponent();
            _targetType = targetType;
            _targetId = targetId;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.IsFrozen)
            {
                MessageBox.Show("Ваш аккаунт заморожен. Вы не можете отправлять жалобы.\n\n" +
                    "Подайте апелляцию в профиле для разморозки аккаунта",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string reason = txtReason.Text.Trim();

            if (string.IsNullOrEmpty(reason))
            {
                ShowStatus("Введите причину жалобы");
                return;
            }

            if (reason.Length < 10)
            {
                ShowStatus("Причина жалобы должна содержать минимум 10 символов");
                return;
            }

            var complaint = new Complaints
            {
                ComplainantId = UserSession.CurrentUser.UserId,
                TargetType = _targetType,
                TargetId = _targetId,
                Reason = reason,
                CreatedAt = DateTime.Now,
                IsResolved = false
            };

            Core.Context.Complaints.Add(complaint);
            Core.Context.SaveChanges();

            MessageBox.Show("Жалоба отправлена администратору", "Успех",
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