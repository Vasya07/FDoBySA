using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class AuthorPage : Page
    {
        private bool _showFrozen = false;

        public AuthorPage()
        {
            InitializeComponent();
            LoadBooks();
        }

        private void LoadBooks()
        {
            if (!UserSession.IsAuthenticated || !UserSession.IsAuthor)
            {
                MessageBox.Show("У вас нет доступа к этой странице", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.GoBack();
                return;
            }

            var query = Core.Context.Books
                .Where(b => b.AuthorId == UserSession.CurrentUser.UserId);

            if (!_showFrozen)
                query = query.Where(b => !b.IsFrozen);

            var books = query
                .Select(b => new
                {
                    b.BookId,
                    b.Title,
                    b.Description,
                    b.CoverPath,
                    b.TextContent,
                    b.IsFrozen,
                    b.FrozenReason,
                    b.CreatedAt
                })
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            BooksGrid.ItemsSource = books;
            btnFrozenBooks.Content = _showFrozen ? "Обычные книги" : "Замороженные книги";
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditBookPage();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadBooks();
            }
        }

        private void EditBook_Click(object sender, RoutedEventArgs e)
        {
            int bookId = (int)((Button)sender).Tag;
            var dialog = new AddEditBookPage(bookId);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                LoadBooks();
            }
        }

        private void ReadBook_Click(object sender, RoutedEventArgs e)
        {
            int bookId = (int)((Button)sender).Tag;
            NavigationService?.Navigate(new BookPage(bookId));
        }

        private async void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            int bookId = (int)((Button)sender).Tag;
            var reviewsCount = Core.Context.Reviews.Count(r => r.BookId == bookId);
            var readingListsCount = Core.Context.ReadingLists.Count(rl => rl.BookId == bookId);
            var complaintsCount = Core.Context.Complaints.Count(c => c.TargetType == "Book" && c.TargetId == bookId);

            string message = $"Удалить книгу?\n\nЭто действие нельзя отменить.\n";

            message += "\n\nПродолжить?";

            var result = MessageBox.Show(message, "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var reviews = Core.Context.Reviews.Where(r => r.BookId == bookId);
                    Core.Context.Reviews.RemoveRange(reviews);

                    var readingLists = Core.Context.ReadingLists.Where(rl => rl.BookId == bookId);
                    Core.Context.ReadingLists.RemoveRange(readingLists);

                    var complaints = Core.Context.Complaints
                        .Where(c => c.TargetType == "Book" && c.TargetId == bookId);
                    Core.Context.Complaints.RemoveRange(complaints);

                    var book = Core.Context.Books.First(b => b.BookId == bookId);
                    Core.Context.Books.Remove(book);

                    await Core.Context.SaveChangesAsync();
                    LoadBooks();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowFrozenBooks_Click(object sender, RoutedEventArgs e)
        {
            _showFrozen = !_showFrozen;
            LoadBooks();
        }
    }
}