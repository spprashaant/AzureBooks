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
    public static class GetBookByID
    {
        [FunctionName("GetBookByID")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "books/{id:int?}")] HttpRequest req, int? id,
            ILogger log)
        {
            string query = $"SELECT * FROM Books WHERE BookID = @BookID;";
            try
            {
                var connStrBuilder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("sqldb_connection"));
                var accessToken = await new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/");
                using (SqlConnection connection = new SqlConnection(connStrBuilder.ConnectionString))
                {
                    connection.AccessToken = accessToken;
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BookID", id);
                        log.LogInformation($"ID: {id}");
                        await connection.OpenAsync();
                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        if (reader.Read())
                        {
                            Book book = new Book
                            {
                                BookID = (int)reader["BookID"],
                                Title = (string)reader["Title"],
                                Author = (string)reader["Author"],
                                PublicationYear = (int)reader["PublicationYear"]
                            };
                            log.LogInformation($"Book: {book.Title}");
                            return new OkObjectResult(book);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError($"Error: {e}");
            }
            return new NotFoundResult();
        }
    }
}
