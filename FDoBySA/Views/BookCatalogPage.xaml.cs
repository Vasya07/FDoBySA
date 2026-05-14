using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class BookCatalogPage : Page
    {
        private System.Windows.Threading.DispatcherTimer _searchTimer;

        public BookCatalogPage()
        {
            InitializeComponent();
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
                if (txtLoading == null || BooksGrid == null)
                {
                    return;
                }

                txtLoading.Visibility = Visibility.Visible;
                BooksGrid.Visibility = Visibility.Collapsed;

                var query = Core.Context.Books
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
                        Genres = b.Genres.Select(g => g.GenreName).ToList()
                    });

                string search = txtSearch.Text.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b => b.Title.Contains(search) ||
                                             b.AuthorName.Contains(search));
                }

                if (cmbGenre.SelectedValue is int genreId)
                {
                    var genreName = Core.Context.Genres
                        .First(g => g.GenreId == genreId).GenreName;
                    query = query.Where(b => b.Genres.Contains(genreName));
                }

                string sort = (cmbSort.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (sort == "По оценке")
                    query = query.OrderByDescending(b => b.AvgRating);
                else
                    query = query.OrderBy(b => b.Title);

                var books = query.ToList();

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
    }
}