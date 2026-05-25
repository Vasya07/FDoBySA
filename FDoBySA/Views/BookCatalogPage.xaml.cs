using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class BookCatalogPage : Page
    {
        public static BookCatalogPage Current { get; private set; }
        private System.Windows.Threading.DispatcherTimer _searchTimer;

        public BookCatalogPage()
        {
            InitializeComponent();
            Current = this;
            LoadGenres();
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
        public void RefreshBooks()
        {
            LoadBooks();
        }
        private void LoadGenres()
        {
            try
            {
                var genres = Core.Context.Genres.OrderBy(g => g.GenreName).ToList();
                var allGenres = new System.Collections.ObjectModel.ObservableCollection<object>();
                allGenres.Add(new { GenreId = (int?)null, GenreName = "Все жанры" });
                foreach (var genre in genres)
                    allGenres.Add(genre);

                cmbGenre.ItemsSource = allGenres;
                cmbGenre.DisplayMemberPath = "GenreName";
                cmbGenre.SelectedValuePath = "GenreId";
                cmbGenre.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки жанров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBooks()
        {
            try
            {
                if (txtLoading == null || BooksGrid == null) return;

                txtLoading.Visibility = Visibility.Visible;
                BooksGrid.Visibility = Visibility.Collapsed;

                var booksFromDb = Core.Context.Books
                    .Where(b => !b.IsFrozen)
                    .Select(b => new
                    {
                        b.BookId,
                        b.Title,
                        b.Description,
                        b.CoverPath,
                        b.TextContent,
                        b.AuthorId,
                        b.IsFrozen,
                        AuthorName = b.Users.DisplayName,
                        AvgRating = b.Reviews.Where(r => !r.IsFrozen)
                                    .Select(r => (double?)r.Rating).Average() ?? 0,
                        Genres = b.Genres.Select(g => g.GenreName)
                    })
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
                    b.AuthorId,
                    b.IsFrozen,
                    b.AuthorName,
                    b.AvgRating,
                    Genres = b.Genres.ToList()
                }).ToList();

                string search = txtSearch.Text.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    books = books.Where(b => b.Title.Contains(search) || b.AuthorName.Contains(search)).ToList();
                }

                if (cmbGenre.SelectedValue is int genreId)
                {
                    var genreName = Core.Context.Genres.First(g => g.GenreId == genreId).GenreName;
                    books = books.Where(b => b.Genres.Contains(genreName)).ToList();
                }

                string sort = (cmbSort.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (sort == "По оценке")
                    books = books.OrderByDescending(b => b.AvgRating).ToList();
                else
                    books = books.OrderBy(b => b.Title).ToList();

                BooksGrid.ItemsSource = books;
                txtLoading.Visibility = Visibility.Collapsed;
                BooksGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                if (txtLoading != null)
                    txtLoading.Visibility = Visibility.Collapsed;

                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void Search_Click(object sender, RoutedEventArgs e) => LoadBooks();

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            cmbGenre.SelectedIndex = 0;
            cmbSort.SelectedIndex = 0;
            LoadBooks();
        }

        private void Sort_Changed(object sender, SelectionChangedEventArgs e) => LoadBooks();

        private void Genre_Changed(object sender, SelectionChangedEventArgs e) => LoadBooks();

        private void ReadBook_Click(object sender, RoutedEventArgs e)
        {
            dynamic book = ((Button)sender).Tag;
            int bookId = book.BookId;
            NavigationService?.Navigate(new BookPage(bookId));
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.IsAuthenticated)
            {
                MessageBox.Show("Авторизуйтесь, чтобы добавлять книги в списки",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic book = ((Button)sender).Tag;
            int bookId = book.BookId;

            var dialog = new SelectListDialog(bookId);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
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