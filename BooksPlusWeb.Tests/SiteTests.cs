using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace BooksPlusWeb.Tests;

// These tests start your whole web app in memory and use it the way a browser would,
// cookies and all. The app reads your own BooksPlus database, so SQL Server must be
// running and BooksPlus created. You don't need to change this file.
[TestClass]
public class SiteTests
{
    private static WebApplicationFactory<Program> factory = null!;

    // The BOOKSPLUS_CONNECTION environment variable, if set, points the tests
    // at another database; otherwise the app uses its own appsettings.json.
    private static string? OtherDatabase => Environment.GetEnvironmentVariable("BOOKSPLUS_CONNECTION");

    [ClassInitialize]
    public static void StartTheApp(TestContext context)
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            if (OtherDatabase is not null)
            {
                builder.UseSetting("ConnectionStrings:BooksPlus", OtherDatabase);
            }
        });
    }

    [ClassCleanup]
    public static void StopTheApp()
    {
        factory.Dispose();
    }

    // A new browser: its own cookies, and redirects shown rather than followed.
    private static HttpClient NewBrowser()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static async Task<string> GetPage(HttpClient browser, string path)
    {
        HttpResponseMessage response = await browser.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET {path} should show a page.");
        return await response.Content.ReadAsStringAsync();
    }

    private static string TokenIn(string html)
    {
        Match token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.IsTrue(token.Success, "The form includes an anti-forgery token.");
        return token.Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> PostForm(HttpClient browser, string formPath, string postPath, Dictionary<string, string> fields)
    {
        string form = await GetPage(browser, formPath);
        fields["__RequestVerificationToken"] = TokenIn(form);
        return await browser.PostAsync(postPath, new FormUrlEncodedContent(fields));
    }

    private static async Task SignIn(HttpClient browser, string userName, string password)
    {
        HttpResponseMessage response = await PostForm(browser, "/Account/Login", "/Account/Login",
            new Dictionary<string, string> { ["userName"] = userName, ["password"] = password });
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, $"Signing in as {userName} redirects.");
    }

    private static int CountCards(string html)
    {
        return Regex.Matches(html, "class=\"card[ \"]").Count;
    }

    [TestMethod]
    public async Task HomePage_StillShowsTheFeaturedBooks()
    {
        string html = await GetPage(NewBrowser(), "/");

        Assert.Contains("Pro SQL Server on Linux", html, "The newest book is featured.");
    }

    [TestMethod]
    public async Task BooksPage_ShowsTwelveBooksAsCards()
    {
        string html = await GetPage(NewBrowser(), "/books");

        Assert.AreEqual(12, CountCards(html), "Page 1 shows 12 books, each in a Bootstrap card.");
        Assert.Contains(".NET Framework Essentials", html, "Books are in order of title, so this one is first.");
        Assert.Contains("/books?page=10", html, "There's a link to the last of 10 pages.");
    }

    [TestMethod]
    public async Task BooksPage_LastPage_ShowsTheLastThreeBooks()
    {
        string html = await GetPage(NewBrowser(), "/books?page=10");

        Assert.AreEqual(3, CountCards(html), "111 books make 9 full pages of 12, and 3 on page 10.");
    }

    [TestMethod]
    public async Task BooksPage_PastTheLastPage_IsNotFound()
    {
        HttpResponseMessage response = await NewBrowser().GetAsync("/books?page=11");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task DetailsPage_IsAtBooksSlashId()
    {
        string html = await GetPage(NewBrowser(), "/books/21");

        Assert.Contains("Fast Track to MDX", html);
        Assert.Contains("$36.58", html);
    }

    [TestMethod]
    [DataRow("/books/9999")]
    [DataRow("/books/abc")]
    public async Task DetailsPage_ForABookThatDoesNotExist_IsNotFound(string path)
    {
        HttpClient browser = NewBrowser();
        await GetPage(browser, "/books/21");

        HttpResponseMessage response = await browser.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task SearchPage_ReadsQFromTheQueryString()
    {
        string html = await GetPage(NewBrowser(), "/books/search?q=Program");

        Assert.Contains("SQL Server 2012 Programming", html);
        Assert.DoesNotContain("Fast Track to MDX", html);
    }

    [TestMethod]
    public async Task CreatePage_SendsVisitorsWhoAreNotSignedInToSignIn()
    {
        HttpResponseMessage response = await NewBrowser().GetAsync("/books/new");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.Contains(response.Headers.Location!.ToString(), "/Account/Login");
    }

    [TestMethod]
    public async Task CreatePage_TurnsAwayStaff()
    {
        HttpClient browser = NewBrowser();
        await SignIn(browser, "clerk", "Bookworm-Clerk-2026");

        HttpResponseMessage response = await browser.GetAsync("/books/new");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.Contains(response.Headers.Location!.ToString(), "/Account/Denied", "Only the Admin role may add books.");
    }

    [TestMethod]
    public async Task CreatePage_ShowsTheFormToAnAdmin()
    {
        HttpClient browser = NewBrowser();
        await SignIn(browser, "admin", "Bookworm-Admin-2026");

        string html = await GetPage(browser, "/books/new");

        Assert.Contains("name=\"Title\"", html);
        Assert.Contains("data-val-required=\"Enter the book&#x27;s title.\"", html, "The Title rule reaches the browser, for client-side validation.");
    }

    [TestMethod]
    public async Task PostingABadBook_ShowsTheMessages_AndSavesNothing()
    {
        HttpClient browser = NewBrowser();
        await SignIn(browser, "admin", "Bookworm-Admin-2026");

        HttpResponseMessage response = await PostForm(browser, "/books/new", "/books/new", new Dictionary<string, string>
        {
            ["Title"] = "",
            ["CategoryId"] = "",
            ["PublishDate"] = "2026-01-15",
            ["Price"] = "0"
        });
        string html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The form comes back.");
        Assert.Contains("Enter the book&#x27;s title.", html);
        Assert.Contains("Choose a category.", html);
        Assert.Contains("Enter a price from $0.01 to $500.", html);
    }

    [TestMethod]
    public async Task PostingATitleThatExists_SaysSo()
    {
        HttpClient browser = NewBrowser();
        await SignIn(browser, "admin", "Bookworm-Admin-2026");

        HttpResponseMessage response = await PostForm(browser, "/books/new", "/books/new", new Dictionary<string, string>
        {
            ["Title"] = "Fast Track to MDX",
            ["CategoryId"] = "3",
            ["PublishDate"] = "2026-01-15",
            ["Price"] = "20"
        });
        string html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("There&#x27;s already a book with that title.", html);
    }

    [TestMethod]
    public async Task PostingAGoodBook_AddsIt_AndRedirectsToIt()
    {
        HttpClient browser = NewBrowser();
        await SignIn(browser, "admin", "Bookworm-Admin-2026");

        try
        {
            HttpResponseMessage response = await PostForm(browser, "/books/new", "/books/new", new Dictionary<string, string>
            {
                ["Title"] = "A Test Book",
                ["CategoryId"] = "1",
                ["PublishDate"] = "2026-01-15",
                ["Price"] = "12.50"
            });

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            string location = response.Headers.Location!.ToString();
            StringAssert.StartsWith(location, "/books/", "It redirects to the new book's page.");
            Assert.Contains("A Test Book", await GetPage(browser, location));
        }
        finally
        {
            string connectionString = OtherDatabase ?? "Server=localhost;Database=BooksPlus;Integrated Security=true;TrustServerCertificate=true;";
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            using SqlCommand command = new SqlCommand("DELETE FROM Book WHERE Title = 'A Test Book';", connection);
            command.ExecuteNonQuery();
        }
    }

    [TestMethod]
    public async Task BooksPage_ShowsRecentlyViewedBooks_NewestFirst()
    {
        HttpClient browser = NewBrowser();
        await GetPage(browser, "/books/21");
        await GetPage(browser, "/books/5");

        string html = await GetPage(browser, "/books");

        int recent = html.IndexOf("Recently viewed", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, recent, "The list shows the books this visitor viewed.");
        int sql2012 = html.IndexOf("SQL Server 2012 Programming", recent, StringComparison.Ordinal);
        int mdx = html.IndexOf("Fast Track to MDX", recent, StringComparison.Ordinal);
        Assert.IsTrue(sql2012 >= 0 && mdx > sql2012, "Book 5, viewed last, comes before book 21.");
    }
}
