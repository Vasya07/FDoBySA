using System;
using System.Linq;
using System.Windows;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtLogin.Text = "vasiliy_glotov";
            txtPassword.Password = "hash_glotov123";
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowStatus("Введите логин и пароль");
                return;
            }

            try
            {
                var user = Core.Context.Users.FirstOrDefault(u => u.Login == login);

                if (user != null && password == user.PasswordHash)
                {
                    UserSession.CurrentUser = user;
                    MainWindow main = new MainWindow();
                    main.Show();
                    Close();
                }
                else
                {
                    ShowStatus("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка подключения: {ex.Message}");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow register = new RegisterWindow();
            register.Owner = this;
            register.ShowDialog();
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