using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FDoBySA.Helpers;

namespace FDoBySA.Views
{
    public partial class AddEditBookPage : Window
    {
        private int? _bookId;
        private List<GenreViewModel> _genres;
        private string _selectedCoverPath = null;

        public AddEditBookPage(int? bookId = null)
        {
            InitializeComponent();
            _bookId = bookId;

            if (bookId.HasValue)
            {
                TitleLabel.Text = "Редактирование книги";
                LoadBookData();
            }

            LoadGenres();
        }

        private void LoadGenres()
        {
            var allGenres = Core.Context.Genres.ToList();
            var bookGenreIds = new List<int>();

            if (_bookId.HasValue)
            {
                var book = Core.Context.Books.First(b => b.BookId == _bookId.Value);
                bookGenreIds = book.Genres.Select(g => g.GenreId).ToList();
            }

            _genres = allGenres.Select(g => new GenreViewModel
            {
                GenreId = g.GenreId,
                GenreName = g.GenreName,
                IsSelected = bookGenreIds.Contains(g.GenreId)
            }).ToList();

            lstGenres.ItemsSource = _genres;
        }

        private void LoadBookData()
        {
            var book = Core.Context.Books.First(b => b.BookId == _bookId.Value);

            txtTitle.Text = book.Title;
            txtDescription.Text = book.Description;
            txtContent.Text = book.TextContent;

            if (!string.IsNullOrEmpty(book.CoverPath))
            {
                _selectedCoverPath = book.CoverPath;
                txtCoverPath.Text = Path.GetFileName(book.CoverPath);

                try
                {
                    var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, book.CoverPath);
                    if (File.Exists(fullPath))
                    {
                        imgPreview.Source = new System.Windows.Media.Imaging.BitmapImage(
                            new Uri(fullPath, UriKind.Absolute));
                    }
                }
                catch { }
            }
        }

        private void SelectCover_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите обложку для книги",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(dialog.FileName);
                string coversDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers");

                if (!Directory.Exists(coversDir))
                    Directory.CreateDirectory(coversDir);

                string destPath = Path.Combine(coversDir, fileName);
                File.Copy(dialog.FileName, destPath, true);

                _selectedCoverPath = "Covers/" + fileName;
                txtCoverPath.Text = fileName;

                imgPreview.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(destPath, UriKind.Absolute));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string title = txtTitle.Text.Trim();
            string description = txtDescription.Text.Trim();
            string content = txtContent.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Введите название книги", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Введите текст книги", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Books book;

            if (_bookId.HasValue)
            {
                book = Core.Context.Books.First(b => b.BookId == _bookId.Value);
                book.Title = title;
                book.Description = description;
                book.TextContent = content;
                if (_selectedCoverPath != null)
                    book.CoverPath = _selectedCoverPath;

                book.Genres.Clear();
            }
            else
            {
                book = new Books
                {
                    Title = title,
                    Description = description,
                    TextContent = content,
                    CoverPath = _selectedCoverPath,
                    AuthorId = UserSession.CurrentUser.UserId,
                    IsFrozen = false,
                    CreatedAt = DateTime.Now
                };
                Core.Context.Books.Add(book);
                Core.Context.SaveChanges();
            }

            var selectedGenres = _genres.Where(g => g.IsSelected).Select(g => g.GenreId).ToList();
            foreach (var genreId in selectedGenres)
            {
                var genre = Core.Context.Genres.First(g => g.GenreId == genreId);
                book.Genres.Add(genre);
            }

            Core.Context.SaveChanges();

            MessageBox.Show("Книга сохранена!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private class GenreViewModel
        {
            public int GenreId { get; set; }
            public string GenreName { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}