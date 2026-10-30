# Exercise: MVC with Dapper

Exercise code for the lesson **Exercise: MVC with Dapper** in the C# Full-Stack Bootcamp on [ask-codey.com](https://www.ask-codey.com).

This is the BooksPlus website from *Exercise: BooksPlus*, ready to grow up. You'll move its data access to Dapper and dependency injection, give it friendly addresses and a paged list of cards, validate the **Add a Book** form, let only administrators add books, and remember the books each visitor has just looked at.

## Before you start

The tests start your website and read your own BooksPlus database, so SQL Server must be running and BooksPlus created (run `BooksPlus.sql` from the course's sample databases if you haven't).

## What's here

- `BooksPlusWeb`: the website as *Exercise: BooksPlus* left it, with the Dapper package added.
- **Ready to use:** `Services/UserService.cs` (two users: `admin` / `Bookworm-Admin-2026` with the Admin role, `clerk` / `Bookworm-Clerk-2026` with the Staff role), `Controllers/AccountController.cs` and its views, `Views/Shared/_LoginStatus.cshtml` (already in the layout) and `Views/Shared/_ValidationScriptsPartial.cshtml`.
- `BooksPlusWeb.Tests`: tests that use your website the way a browser would. You don't need to change these.

## What to do

1. **Dapper.** Rewrite `Data/BookDao.cs` with Dapper, and write its four new methods: `Count`, `GetPage`, `GetByIds` and `TitleExists`.
2. **Dependency injection.** In `Program.cs`, register `BookDao` (scoped, with the `BooksPlus` connection string) and `UserService` (singleton). Change `HomeController` and `BooksController` to ask for a `BookDao` in their constructors.
3. **Sign-in and sessions.** In `Program.cs`, turn on cookie authentication (sign-in page `/Account/Login`, access-denied page `/Account/Denied`) and session state, and add them to the pipeline.
4. **Addresses.** Give `BooksController` attribute routes: `/books` (the list), `/books/{id}` (details, ids only), `/books/search?q=...` (search, reading `q`), and `/books/new` (the form, GET and POST).
5. **The list.** Show 12 books per page, in order of title, each as a Bootstrap card from a partial view `_BookCard`, in a grid of 1, 2 or 3 columns by screen size, with a pager below. `/books?page=2` is page 2; a page past the last is **Not Found**.
6. **Validation.** Add rules to `Book`, with exactly these messages: a title is required (*Enter the book's title.*) and at most 255 characters (*A title can be at most 255 characters.*); a category is required (*Choose a category.*); a price, when given, is from $0.01 to $500 (*Enter a price from $0.01 to $500.*). A title that already exists is refused with *There's already a book with that title.* Show the messages on the form, and turn on client-side validation.
7. **Admins only.** Only the Admin role may open or post the **Add a Book** form.
8. **Recently viewed.** Keep the last three books a visitor opened in the session, and list them, most recent first, under a **Recently viewed** heading at the top of `/books`.

Run the website with **Ctrl+F5**, and the tests with **Test > Run All Tests** (or `dotnet test`).

## Tips

- Steps 1 to 3 come first: until `Program.cs` registers the services, no page that needs a `BookDao` can open.
- With attribute routes, `/Books/Details/21` stops working: that's expected. Tag helpers write the new addresses for you.
- `OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY` needs an `ORDER BY` before it.
- Make `CategoryId` an `int?`, so an unchosen category is `null` and `[Required]` can catch it.
- The session holds text: store the list of ids as JSON with `JsonSerializer`.
- The test that adds a book deletes it again afterward, so your database stays as it was.

## Hand it in

When it works, push your solution to a public repo of your own on GitHub, then paste that repo's address into **Submit your exercise** on the lesson page at [ask-codey.com](https://www.ask-codey.com). That's what completes the lesson. The lesson page shows the Git commands, step by step.
