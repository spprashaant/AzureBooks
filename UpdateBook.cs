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
using Microsoft.Azure.Services.AppAuthentication;

namespace BookCompany.Books
{
    public static class UpdateBook
    {
        [FunctionName("UpdateBook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "books/{id:int}")] HttpRequest req, int id,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Book updatedBook = JsonConvert.DeserializeObject<Book>(requestBody);

            var connStrBuilder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("sqldb_connection"));
            var accessToken = await new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/");
            using (SqlConnection connection = new SqlConnection(connStrBuilder.ConnectionString))
            {
                connection.AccessToken = accessToken;
                connection.Open();
                var text = "UPDATE Books SET Title = @Title, Author = @Author, PublicationYear = @PublicationYear WHERE BookID = @BookID";

                using (SqlCommand cmd = new SqlCommand(text, connection))
                {
                    cmd.Parameters.AddWithValue("@Title", updatedBook.Title);
                    cmd.Parameters.AddWithValue("@Author", updatedBook.Author);
                    cmd.Parameters.AddWithValue("@PublicationYear", updatedBook.PublicationYear);
                    cmd.Parameters.AddWithValue("@BookID", id);

                    var rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                    {
                        return new NotFoundResult();
                    }
                }
            }

            return new OkObjectResult($"Book {id} has been updated successfully.");
        }
    }
}
