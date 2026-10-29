using BooksPlusWeb.Data;
using BooksPlusWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BooksPlusWeb.Controllers;

public class BooksController : Controller
{
    private readonly BookDao bookDao;

    public BooksController(IConfiguration configuration)
    {
        bookDao = new BookDao(configuration.GetConnectionString("BooksPlus")!);
    }

    // GET /Books
    public IActionResult Index()
    {
        List<Book> books = bookDao.GetAll();
        return View(books);
    }

    // GET /Books/Details/5
    public IActionResult Details(int id)
    {
        Book? book = bookDao.GetById(id);
        if (book is null)
        {
            return NotFound();
        }
        return View(book);
    }

    // GET /Books/Search?title=Program
    public IActionResult Search(string? title)
    {
        ViewData["SearchText"] = title;
        List<Book> books = string.IsNullOrWhiteSpace(title) ? new List<Book>() : bookDao.SearchByTitle(title);
        return View(books);
    }

    // GET /Books/Create: shows the empty form
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Categories"] = bookDao.GetCategories();
        return View(new Book { PublishDate = DateTime.Today });
    }

    // POST /Books/Create: saves the form, then shows the new book
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Book book)
    {
        int newId = bookDao.Add(book);
        return RedirectToAction("Details", new { id = newId });
    }
}
