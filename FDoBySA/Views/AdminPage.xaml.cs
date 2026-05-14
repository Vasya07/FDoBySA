using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            InitializeComponent();

            if (!UserSession.IsAdmin)
            {
                MessageBox.Show("У вас нет доступа к этой странице", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.GoBack();
                return;
            }

            LoadComplaints();
            LoadAuthorRequests();
            LoadAppeals();
            LoadUsers();
        }

        private void LoadComplaints()
        {
            var complaints = Core.Context.Complaints
                .Where(c => !c.IsResolved)
                .Select(c => new
                {
                    c.ComplaintId,
                    c.TargetType,
                    c.TargetId,
                    c.Reason,
                    c.CreatedAt,
                    ComplainantName = c.Users.DisplayName
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            ComplaintsGrid.ItemsSource = complaints;
        }

        private void LoadAuthorRequests()
        {
            var requests = Core.Context.AuthorRequests
                .Where(r => !r.IsProcessed)
                .Select(r => new
                {
                    r.RequestId,
                    UserName = r.Users.DisplayName,
                    UserLogin = r.Users.Login,
                    r.RequestDate
                })
                .OrderBy(r => r.RequestDate)
                .ToList();

            AuthorRequestsGrid.ItemsSource = requests;
        }

        private void LoadAppeals()
        {
            var appeals = Core.Context.UnfreezeRequests
                .Where(r => !r.IsProcessed)
                .Select(r => new
                {
                    r.RequestId,
                    UserName = r.Users.DisplayName,
                    r.TargetType,
                    r.TargetId,
                    r.Reason,
                    r.RequestDate
                })
                .OrderBy(r => r.RequestDate)
                .ToList();

            AppealsGrid.ItemsSource = appeals;
        }

        private void LoadUsers()
        {
            var users = Core.Context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Login,
                    u.DisplayName,
                    u.Email,
                    u.IsFrozen,
                    RoleName = u.Roles.RoleName,
                    RoleId = u.RoleId,
                    Status = u.IsFrozen ? "Заморожен" : "Активен"
                })
                .OrderBy(u => u.DisplayName)
                .ToList();

            UsersGrid.ItemsSource = users;
        }

        private void AcceptComplaint_Click(object sender, RoutedEventArgs e)
        {
            int complaintId = (int)((Button)sender).Tag;
            var complaint = Core.Context.Complaints.First(c => c.ComplaintId == complaintId);
            complaint.IsResolved = true;

            if (complaint.TargetType == "Book")
            {
                var book = Core.Context.Books.First(b => b.BookId == complaint.TargetId);
                book.IsFrozen = true;
                book.FrozenReason = $"Заморожено по жалобе #{complaintId}";
            }
            else if (complaint.TargetType == "Review")
            {
                var review = Core.Context.Reviews.First(r => r.ReviewId == complaint.TargetId);
                review.IsFrozen = true;
            }

            Core.Context.SaveChanges();
            LoadComplaints();
        }

        private void RejectComplaint_Click(object sender, RoutedEventArgs e)
        {
            int complaintId = (int)((Button)sender).Tag;
            var complaint = Core.Context.Complaints.First(c => c.ComplaintId == complaintId);
            complaint.IsResolved = true;
            Core.Context.SaveChanges();
            LoadComplaints();
        }

        private void AcceptAuthorRequest_Click(object sender, RoutedEventArgs e)
        {
            int requestId = (int)((Button)sender).Tag;
            var request = Core.Context.AuthorRequests.First(r => r.RequestId == requestId);
            request.IsProcessed = true;
            request.ProcessedAt = DateTime.Now;

            var user = request.Users;
            user.RoleId = 2;

            Core.Context.SaveChanges();
            LoadAuthorRequests();

            MessageBox.Show($"Пользователь {user.DisplayName} назначен автором",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectAuthorRequest_Click(object sender, RoutedEventArgs e)
        {
            int requestId = (int)((Button)sender).Tag;
            var request = Core.Context.AuthorRequests.First(r => r.RequestId == requestId);
            request.IsProcessed = true;
            request.ProcessedAt = DateTime.Now;
            Core.Context.SaveChanges();
            LoadAuthorRequests();
        }

        private void AcceptAppeal_Click(object sender, RoutedEventArgs e)
        {
            int requestId = (int)((Button)sender).Tag;
            var request = Core.Context.UnfreezeRequests.First(r => r.RequestId == requestId);
            request.IsProcessed = true;
            request.ProcessedAt = DateTime.Now;

            if (request.TargetType == "Account")
            {
                var user = request.Users;
                user.IsFrozen = false;
                user.FrozenReason = null;
            }
            else if (request.TargetType == "Book")
            {
                var book = Core.Context.Books.First(b => b.BookId == request.TargetId);
                book.IsFrozen = false;
                book.FrozenReason = null;
            }

            Core.Context.SaveChanges();
            LoadAppeals();
            UserSession.RefreshUser();
        }

        private void RejectAppeal_Click(object sender, RoutedEventArgs e)
        {
            int requestId = (int)((Button)sender).Tag;
            var request = Core.Context.UnfreezeRequests.First(r => r.RequestId == requestId);
            request.IsProcessed = true;
            request.ProcessedAt = DateTime.Now;
            Core.Context.SaveChanges();
            LoadAppeals();
        }

        private void UserSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadUsers();
        }

        private void Role_Changed(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null || combo.SelectedItem == null) return;

            int userId = (int)combo.Tag;
            int newRoleId = int.Parse(((ComboBoxItem)combo.SelectedItem).Tag.ToString());

            var user = Core.Context.Users.First(u => u.UserId == userId);
            user.RoleId = newRoleId;
            Core.Context.SaveChanges();

            LoadUsers();
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            int userId = (int)((Button)sender).Tag;
            var user = Core.Context.Users.First(u => u.UserId == userId);

            string newPassword = PasswordHelper.GenerateRandomPassword();
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            Core.Context.SaveChanges();

            MessageBox.Show($"Новый пароль для пользователя {user.Login}: {newPassword}\n\n" +
                "Сохраните пароль и передайте его пользователю",
                "Пароль сброшен", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}