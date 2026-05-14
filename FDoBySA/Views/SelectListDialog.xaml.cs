using FDoBySA.Helpers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FDoBySA.Views
{
    public partial class SelectListDialog : Window
    {
        private int _bookId;

        public SelectListDialog(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;

            var existing = Core.Context.ReadingLists
                .FirstOrDefault(rl => rl.UserId == UserSession.CurrentUser.UserId &&
                                      rl.BookId == _bookId);

            if (existing != null)
            {
                foreach (ListBoxItem item in listStatus.Items)
                {
                    if (item.Tag.ToString() == existing.Status)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (listStatus.SelectedItem == null)
            {
                MessageBox.Show("Выберите список", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string status = ((ListBoxItem)listStatus.SelectedItem).Tag.ToString();

            var existing = Core.Context.ReadingLists
                .FirstOrDefault(rl => rl.UserId == UserSession.CurrentUser.UserId &&
                                      rl.BookId == _bookId);

            if (existing != null)
            {
                existing.Status = status;
            }
            else
            {
                var readingList = new ReadingLists
                {
                    UserId = UserSession.CurrentUser.UserId,
                    BookId = _bookId,
                    Status = status,
                    AddedAt = System.DateTime.Now
                };
                Core.Context.ReadingLists.Add(readingList);
            }

            Core.Context.SaveChanges();

            MessageBox.Show("Книга добавлена в список", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}