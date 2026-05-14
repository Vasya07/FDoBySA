using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class UserProfilePage : Page
    {
        public UserProfilePage()
        {
            InitializeComponent();
            LoadUserData();
            LoadUserReviews();
            CheckAuthorRequest();
        }

        private void LoadUserData()
        {
            if (!UserSession.IsAuthenticated)
            {
                NavigationService?.Navigate(new LoginWindow());
                return;
            }

            var user = UserSession.CurrentUser;

            txtLogin.Text = user.Login;
            txtDisplayName.Text = user.DisplayName;
            txtEmail.Text = user.Email;
            txtRole.Text = user.RoleId == 3 ? "Администратор" :
                          (user.RoleId == 2 ? "Автор" : "Читатель");
            txtCreatedAt.Text = user.CreatedAt?.ToString("dd.MM.yyyy") ?? "Не указана";
            txtStatus.Text = user.IsFrozen ? "Заморожен" : "Активен";

            if (user.IsFrozen)
            {
                FrozenWarning.Visibility = Visibility.Visible;
                txtFrozenReason.Text = $"Причина: {user.FrozenReason}";
                AuthorRequestPanel.Visibility = Visibility.Collapsed;
            }
            else if (user.RoleId == 2 || user.RoleId == 3)
            {
                AuthorRequestPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadUserReviews()
        {
            var reviews = Core.Context.Reviews
                .Where(r => r.UserId == UserSession.CurrentUser.UserId)
                .Select(r => new
                {
                    r.ReviewId,
                    r.Rating,
                    r.ReviewText,
                    r.CreatedAt,
                    r.IsFrozen,
                    BookTitle = r.Books.Title
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            if (reviews.Count == 0)
            {
                ReviewsGrid.Visibility = Visibility.Collapsed;
                txtNoReviews.Visibility = Visibility.Visible;
            }
            else
            {
                ReviewsGrid.Visibility = Visibility.Visible;
                txtNoReviews.Visibility = Visibility.Collapsed;
                ReviewsGrid.ItemsSource = reviews;
            }
        }

        private void CheckAuthorRequest()
        {
            var existingRequest = Core.Context.AuthorRequests
                .FirstOrDefault(r => r.UserId == UserSession.CurrentUser.UserId &&
                                     !r.IsProcessed);

            if (existingRequest != null)
            {
                btnRequestAuthor.Visibility = Visibility.Collapsed;
                txtRequestStatus.Visibility = Visibility.Visible;
                txtRequestStatus.Text = "Заявка на роль автора уже подана и ожидает рассмотрения";
            }
        }

        private void RequestAuthor_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите подать заявку на роль автора?\n\n" +
                "После одобрения вы сможете публиковать свои книги",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var request = new AuthorRequests
                {
                    UserId = UserSession.CurrentUser.UserId,
                    RequestDate = DateTime.Now,
                    IsProcessed = false
                };

                Core.Context.AuthorRequests.Add(request);
                Core.Context.SaveChanges();

                btnRequestAuthor.Visibility = Visibility.Collapsed;
                txtRequestStatus.Visibility = Visibility.Visible;
                txtRequestStatus.Text = "Заявка успешно подана! Ожидайте решения администратора";
            }
        }

        private void Appeal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AppealDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Апелляция подана. Администратор рассмотрит вашу заявку",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteReview_Click(object sender, RoutedEventArgs e)
        {
            int reviewId = (int)((Button)sender).Tag;

            var result = MessageBox.Show("Удалить этот отзыв?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var review = Core.Context.Reviews.First(r => r.ReviewId == reviewId);
                Core.Context.Reviews.Remove(review);
                Core.Context.SaveChanges();
                LoadUserReviews();
            }
        }
    }
}