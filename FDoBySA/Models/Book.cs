using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDoBySA.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverPath { get; set; }
        public string TextContent { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public bool IsFrozen { get; set; }
        public double AvgRating { get; set; }
        public List<string> Genres { get; set; } = new List<string>();
    }
}
