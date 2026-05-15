using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class ReadingListsPage : Page
    {
        private string _currentStatus = "Читаю";
        private System.Windows.Threading.DispatcherTimer _searchTimer;

        public ReadingListsPage()
        {
            InitializeComponent();

            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Авторизуйтесь для просмотра списков книг",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService?.GoBack();
                return;
            }

            LoadBooks();

            _searchTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                LoadBooks();
            };
        }

        private void LoadBooks()
        {
            try
            {
                if (BooksGrid == null || txtEmpty == null) return;
                var query = from rl in Core.Context.ReadingLists
                            where rl.UserId == UserSession.CurrentUser.UserId
                            where rl.Status == _currentStatus
                            join b in Core.Context.Books on rl.BookId equals b.BookId
                            where !b.IsFrozen
                            select new BookListItem
                            {
                                BookId = b.BookId,
                                Title = b.Title,
                                CoverPath = b.CoverPath,
                                AuthorName = b.Users.DisplayName,
                                Status = rl.Status,
                                AvgRating = b.Reviews.Where(r => !r.IsFrozen)
                                            .Average(r => (double?)r.Rating) ?? 0
                            };

                string search = txtSearch?.Text.Trim() ?? "";
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b => b.Title.Contains(search) ||
                                             b.AuthorName.Contains(search));
                }

                string sort = (cmbSort?.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (sort == "По оценке")
                    query = query.OrderByDescending(b => b.AvgRating);
                else
                    query = query.OrderBy(b => b.Title);

                var booksFromDb = query.ToList();
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var books = booksFromDb.Select(b => new BookListItem
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    CoverPath = string.IsNullOrEmpty(b.CoverPath)
                        ? null
                        : Path.Combine(baseDir, b.CoverPath.Replace('/', '\\')),
                    AuthorName = b.AuthorName,
                    Status = b.Status,
                    AvgRating = b.AvgRating
                }).ToList();

                if (books.Count == 0)
                {
                    BooksGrid.Visibility = Visibility.Collapsed;
                    txtEmpty.Visibility = Visibility.Visible;
                }
                else
                {
                    BooksGrid.Visibility = Visibility.Visible;
                    txtEmpty.Visibility = Visibility.Collapsed;
                    BooksGrid.ItemsSource = books;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списков: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem)
                {
                    _currentStatus = tabItem.Tag.ToString();
                    LoadBooks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения вкладки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                if (combo?.DataContext != null)
                {
                    dynamic item = combo.DataContext;
                    string currentStatus = item.Status;

                    foreach (ComboBoxItem cbItem in combo.Items)
                    {
                        if (cbItem.Content.ToString() == currentStatus)
                        {
                            combo.SelectedItem = cbItem;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ComboBox error: {ex.Message}");
            }
        }

        private void Status_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var combo = sender as ComboBox;
                if (combo == null || combo.SelectedItem == null) return;

                int bookId = (int)combo.Tag;
                string newStatus = (combo.SelectedItem as ComboBoxItem)?.Content.ToString();

                var readingList = Core.Context.ReadingLists
                    .FirstOrDefault(rl => rl.UserId == UserSession.CurrentUser.UserId &&
                                          rl.BookId == bookId);

                if (readingList != null && readingList.Status != newStatus)
                {
                    readingList.Status = newStatus;
                    Core.Context.SaveChanges();
                    LoadBooks();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения статуса: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cmbSort.SelectedIndex = 0;
            LoadBooks();
        }

        private void Sort_Changed(object sender, SelectionChangedEventArgs e) => LoadBooks();

        private void ReadBook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int bookId = (int)((Button)sender).Tag;
                NavigationService?.Navigate(new BookPage(bookId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия книги: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

    public class BookListItem
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string CoverPath { get; set; }
        public string AuthorName { get; set; }
        public string Status { get; set; }
        public double AvgRating { get; set; }
    }
}