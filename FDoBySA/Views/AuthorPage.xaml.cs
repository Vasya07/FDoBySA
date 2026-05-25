using System;
using System.IO;
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

            var booksFromDb = query
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

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var books = booksFromDb.Select(b => new
            {
                b.BookId,
                b.Title,
                b.Description,
                CoverPath = string.IsNullOrEmpty(b.CoverPath)
                    ? null
                    : Path.Combine(baseDir, b.CoverPath.Replace('/', '\\')),
                b.TextContent,
                b.IsFrozen,
                b.FrozenReason,
                b.CreatedAt
            }).ToList();

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

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            int bookId = (int)((Button)sender).Tag;

            var result = MessageBox.Show("Удалить эту книгу?\n\nЭто действие нельзя отменить.\n\n" +
                "Все связанные отзывы и записи в списках также будут удалены",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var reviews = Core.Context.Reviews.Where(r => r.BookId == bookId).ToList();
                    foreach (var review in reviews)
                    {
                        Core.Context.Reviews.Remove(review);
                    }

                    var readingLists = Core.Context.ReadingLists.Where(rl => rl.BookId == bookId).ToList();
                    foreach (var readingList in readingLists)
                    {
                        Core.Context.ReadingLists.Remove(readingList);
                    }

                    var complaints = Core.Context.Complaints
                        .Where(c => c.TargetType == "Book" && c.TargetId == bookId).ToList();
                    foreach (var complaint in complaints)
                    {
                        Core.Context.Complaints.Remove(complaint);
                    }

                    var book = Core.Context.Books.First(b => b.BookId == bookId);
                    Core.Context.Books.Remove(book);

                    Core.Context.SaveChanges();

                    LoadBooks();

                    MessageBox.Show("Книга успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var img = sender as Image;
            if (img != null)
            {
                img.Source = null;
            }
        }
    }
}