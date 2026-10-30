using BooksPlusWeb.Models;
using Microsoft.Data.SqlClient;

namespace BooksPlusWeb.Data;

// The data access for books and categories. Rewrite it with Dapper, and write the four new methods.
public class BookDao
{
    private const string BookColumns = "BookId, CategoryId, Title, PublishDate, Price, Summary";
    private readonly string connectionString;

    public BookDao(string connectionString)
    {
        this.connectionString = connectionString;
    }

    // The newest books added to the store, newest first.
    public List<Book> GetFeatured(int count)
    {
        return QueryBooks($"SELECT TOP (@Count) {BookColumns} FROM Book ORDER BY BookId DESC;", "@Count", count);
    }

    // Every book, in alphabetical order of title.
    public List<Book> GetAll()
    {
        return QueryBooks($"SELECT {BookColumns} FROM Book ORDER BY Title;");
    }

    // The book with this id, or null if there isn't one.
    public Book? GetById(int bookId)
    {
        List<Book> books = QueryBooks($"SELECT {BookColumns} FROM Book WHERE BookId = @BookId;", "@BookId", bookId);
        return books.Count > 0 ? books[0] : null;
    }

    // Every book whose title contains the text, in alphabetical order of title.
    public List<Book> SearchByTitle(string text)
    {
        return QueryBooks($"SELECT {BookColumns} FROM Book WHERE Title LIKE '%' + @Text + '%' ORDER BY Title;", "@Text", text);
    }

    // Adds a book and returns its new BookId.
    public int Add(Book book)
    {
        string sql = """
            INSERT INTO Book (CategoryId, Title, PublishDate, Price, Summary)
            VALUES (@CategoryId, @Title, @PublishDate, @Price, @Summary);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
        using SqlCommand command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CategoryId", book.CategoryId);
        command.Parameters.AddWithValue("@Title", book.Title);
        command.Parameters.AddWithValue("@PublishDate", book.PublishDate);
        command.Parameters.AddWithValue("@Price", (object?)book.Price ?? DBNull.Value);
        command.Parameters.AddWithValue("@Summary", (object?)book.Summary ?? DBNull.Value);
        return (int)command.ExecuteScalar();
    }

    // Every category, in alphabetical order of name.
    public List<Category> GetCategories()
    {
        List<Category> categories = new List<Category>();

        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
        using SqlCommand command = new SqlCommand("SELECT CategoryId, CategoryName FROM Category ORDER BY CategoryName;", connection);
        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(new Category { CategoryId = reader.GetInt32(0), CategoryName = reader.GetString(1) });
        }
        return categories;
    }

    // How many books there are.
    public int Count()
    {
        throw new NotImplementedException();
    }

    // One page of books, in alphabetical order of title. Page 1 is the first page.
    public List<Book> GetPage(int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    // The books with these ids, in any order. Ids with no book are skipped.
    public List<Book> GetByIds(List<int> bookIds)
    {
        throw new NotImplementedException();
    }

    // Whether a book with exactly this title exists.
    public bool TitleExists(string title)
    {
        throw new NotImplementedException();
    }

    private List<Book> QueryBooks(string sql, string? parameterName = null, object? value = null)
    {
        List<Book> books = new List<Book>();

        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
        using SqlCommand command = new SqlCommand(sql, connection);
        if (parameterName is not null)
        {
            command.Parameters.AddWithValue(parameterName, value);
        }

        using SqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            books.Add(new Book
            {
                BookId = (int)reader["BookId"],
                CategoryId = (int)reader["CategoryId"],
                Title = (string)reader["Title"],
                PublishDate = (DateTime)reader["PublishDate"],
                Price = reader["Price"] as decimal?,
                Summary = reader["Summary"] as string
            });
        }
        return books;
    }
}
