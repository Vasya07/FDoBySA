using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class BookPage : Page
    {
        private int _bookId;
        private Books _book;

        public BookPage(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            LoadBookData();
            LoadReviews();

            if (UserSession.IsAdmin)
            {
                btnFreeze.Visibility = Visibility.Visible;
            }
        }

        private void LoadBookData()
        {
            _book = Core.Context.Books.FirstOrDefault(b => b.BookId == _bookId);

            if (_book == null)
            {
                MessageBox.Show("Книга не найдена", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService?.GoBack();
                return;
            }

            BookTitle.Text = _book.Title;
            BookAuthor.Text = _book.Users.DisplayName;
            BookDescription.Text = _book.Description ?? "Нет описания";
            BookContent.Text = _book.TextContent;

            if (!string.IsNullOrEmpty(_book.CoverPath))
            {
                try
                {
                    BookCover.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(_book.CoverPath, UriKind.Relative));
                }
                catch { }
            }

            var avgRating = Core.Context.Reviews
                .Where(r => r.BookId == _bookId && !r.IsFrozen)
                .Average(r => (double?)r.Rating) ?? 0;
            BookRating.Text = avgRating.ToString("F1");

            var genres = Core.Context.Books
                .Where(b => b.BookId == _bookId)
                .SelectMany(b => b.Genres.Select(g => g.GenreName))
                .ToList();

            foreach (var genre in genres)
            {
                var genreBadge = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(224, 224, 224)),
                    CornerRadius = new System.Windows.CornerRadius(3),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 5, 5)
                };
                genreBadge.Child = new TextBlock
                {
                    Text = genre,
                    FontSize = 11
                };
                GenresPanel.Children.Add(genreBadge);
            }
        }

        private void LoadReviews()
        {
            var reviews = Core.Context.Reviews
                .Where(r => r.BookId == _bookId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReviewId,
                    r.Rating,
                    r.ReviewText,
                    r.CreatedAt,
                    r.IsFrozen,
                    UserName = r.Users.DisplayName
                })
                .ToList();

            ReviewsGrid.ItemsSource = reviews;
        }

        private void AddReview_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Авторизуйтесь, чтобы оставить отзыв", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UserSession.IsFrozen)
            {
                MessageBox.Show("Ваш аккаунт заморожен. Вы не можете оставлять отзывы",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int rating = int.Parse((cmbRating.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "7");
            string reviewText = txtReview.Text.Trim();

            if (string.IsNullOrEmpty(reviewText))
            {
                MessageBox.Show("Введите текст отзыва", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var review = new Reviews
            {
                BookId = _bookId,
                UserId = UserSession.CurrentUser.UserId,
                Rating = rating,
                ReviewText = reviewText,
                CreatedAt = DateTime.Now,
                IsFrozen = false
            };

            Core.Context.Reviews.Add(review);
            Core.Context.SaveChanges();

            MessageBox.Show("Отзыв добавлен!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            txtReview.Clear();
            cmbRating.SelectedIndex = 4;
            LoadReviews();
            LoadBookData();
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Авторизуйтесь, чтобы добавлять книги в списки",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SelectListDialog(_bookId);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void Complaint_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Авторизуйтесь, чтобы подать жалобу",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UserSession.IsFrozen)
            {
                MessageBox.Show("Ваш аккаунт заморожен. Вы не можете отправлять жалобы.\n\n" +
                    "Подайте апелляцию в профиле для разморозки аккаунта",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new ComplaintDialog("Book", _bookId);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void ComplaintOnReview_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Авторизуйтесь, чтобы подать жалобу",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UserSession.IsFrozen)
            {
                MessageBox.Show("Ваш аккаунт заморожен. Вы не можете отправлять жалобы.\n\n" +
                    "Подайте апелляцию в профиле для разморозки аккаунта",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int reviewId = (int)((Button)sender).Tag;
            var dialog = new ComplaintDialog("Review", reviewId);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void FreezeBook_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Заморозить книгу \"{_book.Title}\"?\n\n" +
                "После заморозки книга будет скрыта из каталога",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _book.IsFrozen = true;
                _book.FrozenReason = "Заморожено администратором";
                Core.Context.SaveChanges();

                MessageBox.Show("Книга заморожена", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService?.GoBack();
            }
        }

        private void FreezeReview_Click(object sender, RoutedEventArgs e)
        {
            int reviewId = (int)((Button)sender).Tag;
            var review = Core.Context.Reviews.First(r => r.ReviewId == reviewId);

            var result = MessageBox.Show($"Заморозить отзыв пользователя {review.Users.DisplayName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                review.IsFrozen = true;
                Core.Context.SaveChanges();

                MessageBox.Show("Отзыв заморожен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadReviews();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}