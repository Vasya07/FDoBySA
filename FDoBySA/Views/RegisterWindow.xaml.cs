using System;
using System.Linq;
using System.Windows;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;
            string email = txtEmail.Text.Trim();
            string displayName = txtDisplayName.Text.Trim();

            if (string.IsNullOrEmpty(login))
            {
                ShowStatus("Введите логин");
                return;
            }

            if (login.Length < 3)
            {
                ShowStatus("Логин должен содержать минимум 3 символа");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowStatus("Введите пароль");
                return;
            }

            if (password.Length < 4)
            {
                ShowStatus("Пароль должен содержать минимум 4 символа");
                return;
            }

            if (password != confirmPassword)
            {
                ShowStatus("Пароли не совпадают");
                return;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
            {
                ShowStatus("Введите корректный email");
                return;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                ShowStatus("Введите отображаемое имя");
                return;
            }

            try
            {
                if (Core.Context.Users.Any(u => u.Login == login))
                {
                    ShowStatus("Пользователь с таким логином уже существует");
                    return;
                }

                if (Core.Context.Users.Any(u => u.Email == email))
                {
                    ShowStatus("Пользователь с таким email уже существует");
                    return;
                }

                var user = new Users
                {
                    Login = login,
                    PasswordHash = password,
                    Email = email,
                    DisplayName = displayName,
                    RoleId = 1,
                    IsFrozen = false,
                    CreatedAt = DateTime.Now
                };

                Core.Context.Users.Add(user);
                Core.Context.SaveChanges();

                MessageBox.Show("Регистрация успешна! Теперь вы можете войти.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}");
            }
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