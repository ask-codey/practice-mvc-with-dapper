namespace BooksPlusWeb.Models;

// One row of the Book table.
public class Book
{
    public int BookId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = "";
    public DateTime PublishDate { get; set; }
    public decimal? Price { get; set; }
    public string? Summary { get; set; }
}
