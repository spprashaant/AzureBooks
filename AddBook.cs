using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Azure.Services.AppAuthentication;

namespace BookCompany.Books
{
    public class Book
    {
        public int BookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int PublicationYear { get; set; }
    }
    public static class AddBook
    {
        [FunctionName("AddBook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, [Queue("newbooknotifications", Connection = "AzureWebJobsStorage")] ICollector<string> outQueueItem,
            ILogger log)
        {
            try{
            // Get the request body and convert it to a Book object
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Book newBook = JsonConvert.DeserializeObject<Book>(requestBody);

            // You can now use newBook.Title, newBook.Author, etc. in your code.
            // Add your code here to insert the new book data into the database.
            var text = $"INSERT INTO Books (Title, Author, PublicationYear) VALUES (@Title, @Author, @PublicationYear)";
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            int result = 0;
            var connStrBuilder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("sqldb_connection"));
            var accessToken = await new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/");
            using (SqlConnection conn = new SqlConnection(connStrBuilder.ConnectionString))
            {
                conn.AccessToken = accessToken;
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Add the parameters for the InsertCommand.
                    cmd.Parameters.AddWithValue("@Title", newBook.Title);
                    cmd.Parameters.AddWithValue("@Author", newBook.Author);
                    cmd.Parameters.AddWithValue("@PublicationYear", newBook.PublicationYear);

                    // Execute the command and log the # rows affected.
                    result = await cmd.ExecuteNonQueryAsync();
                };
            }
            // If adding the book is successful, add a message to the queue
            outQueueItem.Add(JsonConvert.SerializeObject(newBook));
            // After inserting the book, return a success message
            return new OkObjectResult($"Book {newBook.Title} by {newBook.Author} added successfully. Rows affected: {result}");
            }
            catch(Exception ex)
            {
                return new OkObjectResult($"Error: {ex.Message}");
            }
        }
    }
}
