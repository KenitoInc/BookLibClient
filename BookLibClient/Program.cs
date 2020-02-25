using BookLibClient.BookLibService.Models;
using BookLibClient.Default;
using Microsoft.OData.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookLibClient
{
    class Program
    {
        private static async Task AddBook(Container dsc, Book book)
        {
            dsc.AddToBooks(book);
            await dsc.SaveChangesAsync();
        }
  
        private static async Task ListAllBooks(Container dsc)
        {
            var books = await dsc.Books.ExecuteAsync();
            foreach(Book bk in books)
            {
                Console.WriteLine($"Title: {bk.Title} ISBN: {bk.Isbn} Year: {bk.Year}");
            }
        }

        private static async Task GetBookById(Container dsc, int id)
        {
            var book1 = dsc.Books.Where(b => b.Id == id).First();
            Console.WriteLine($"Book1 Title: {book1.Title} ISBN: {book1.Isbn} Year: {book1.Year}");

            var book2 = await dsc.Books.ByKey(id).GetValueAsync();
            Console.WriteLine($"Book2 Title: {book2.Title} ISBN: {book2.Isbn} Year: {book2.Year}");
        }

        private static async Task GetBooksByYear(Container dsc, int year)
        {
            var books = dsc.Books.Where(b => b.Year == year);
            foreach (Book bk in books)
            {
                Console.WriteLine($"Title: {bk.Title} ISBN: {bk.Isbn} Year: {bk.Year}");
            }
        }

        private static async Task BatchRequestsForChangedEntries(Container dsc)
        {
            var book1 = dsc.Books.Where(b => b.Id == 1).First();
            var book2 = dsc.Books.Where(b => b.Id == 2).First();

            book1.Year = 2010;

            dsc.UpdateObject(book1);
            dsc.DeleteObject(book2);

            DataServiceResponse response = await dsc.SaveChangesAsync(SaveChangesOptions.BatchWithIndependentOperations);
            Console.WriteLine($"IsBatch {response.IsBatchResponse} Status Code: {response.BatchStatusCode}");

            foreach(ChangeOperationResponse cor in response)
            {
                Console.WriteLine($"Statuc Code: {cor.StatusCode} Error: {cor.Error}");
            }

        }

        private static async Task ListUsersAndBooksByLoadProperty(Container dsc)
        {
            var users = await dsc.Users.ExecuteAsync();
            var usersBooks = await dsc.LoadPropertyAsync(users, "BooksRead");

            foreach(User user in usersBooks)
            {
                Console.WriteLine($"User: {user.Name}");
                foreach(Book book in user.BooksRead)
                {
                    Console.WriteLine($"Book: {book.Title}");
                }
            }
        }

        private static async Task ListUsersAndBooksByQueryOption(Container dsc)
        {
            var usersBooks = await dsc.Users.AddQueryOption("$expand", "BooksRead").ExecuteAsync();

            foreach (User user in usersBooks)
            {
                Console.WriteLine($"User: {user.Name}");
                foreach (Book book in user.BooksRead)
                {
                    Console.WriteLine($"Book: {book.Title}");
                }
            }
        }

        private static async Task BatchRequestsQueries(Container dsc, Uri serviceUri)
        {
            Uri usersUri = new Uri(serviceUri.AbsoluteUri + "/users");
            Uri booksUri = new Uri(serviceUri.AbsoluteUri + "/books");

            DataServiceRequest<User> usersQuery = new DataServiceRequest<User>(usersUri);
            DataServiceRequest<Book> booksQuery = new DataServiceRequest<Book>(booksUri);
            DataServiceRequest[] batchRequests = new DataServiceRequest[] { usersQuery, booksQuery };

            DataServiceResponse batchResponse;

            try
            {
                batchResponse = await dsc.ExecuteBatchAsync(batchRequests);
                Console.WriteLine($"Is batch response: {batchResponse.IsBatchResponse}");

                foreach(QueryOperationResponse response in batchResponse)
                {
                    if(response.StatusCode < 200 || response.StatusCode > 299)
                    {
                        Console.WriteLine($"Error Message : {response.Error.Message} Status Code: {response.StatusCode}");
                    }
                    else
                    {
                        if(response.Query.ElementType == typeof(User))
                        {
                            foreach(User user in response)
                            {
                                Console.WriteLine($"User: {user.Name}");
                            }
                        }
                        else if(response.Query.ElementType == typeof(Book))
                        {
                            foreach (Book book in response)
                            {
                                Console.WriteLine($"Book: {book.Title}");
                            }
                        }
                    }
                }
            }
            catch(DataServiceRequestException ex)
            {
                batchResponse = ex.Response;
                foreach (QueryOperationResponse response in batchResponse)
                {
                    if (response.Error != null)
                    {
                        Console.WriteLine("An error occurred.");
                        Console.WriteLine(response);
                    }
                }
            }
        }

        private static async Task GetAllUsers(Container dsc)
        {
            DataServiceQuery<User> query = dsc.CreateQuery<User>("Users");
            query = query.IncludeTotalCount();
            //var response1 = await query.ExecuteAsync();
            QueryOperationResponse<User> response = await query.ExecuteAsync() as QueryOperationResponse<User>;

            Console.WriteLine($"There are a total of {response.TotalCount} Users.");
            foreach (var res in response)
            {
                Console.WriteLine(res.Name);
            }
        }

        private static async Task GetAuthorsAndBooks(Container dsc)
        {
            DataServiceQuery<Author> query = dsc.CreateQuery<Author>("Authors");
            var response = await query.AddQueryOption("$expand", "Books").ExecuteAsync() as QueryOperationResponse<Author>;

            foreach (Author author in response)
            {
                Console.WriteLine($"Author: {author.Name}");
                foreach (Book book in author.Books)
                {
                    Console.WriteLine($"Book: {book.Title}");
                }
            }
        }
        static void Main(string[] args)
        {
            string uri = "";
            Uri serviceUri = new Uri(uri);
            Container dsc = new Container(serviceUri);
        }
    }
}
