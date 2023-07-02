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
using System.Collections.Generic;
using Microsoft.Azure.Services.AppAuthentication;

namespace BookCompany.Books
{
    public static class GetBooks
    {
        [FunctionName("GetBooks")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var books = new List<Book>();
            var connStrBuilder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("sqldb_connection"));
            var accessToken = await new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/");
            using (SqlConnection conn = new SqlConnection(connStrBuilder.ConnectionString))
            {
                conn.AccessToken = accessToken;
                conn.Open();
                var text = "SELECT * FROM Books";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    while (reader.Read())
                    {
                        log.LogInformation($"BookID: {reader["BookID"]}, Title: {reader["Title"]}, Author: {reader["Author"]}, PublicationYear: {reader["PublicationYear"]}");
                        //books.Add($"BookID: {reader["BookID"]}, Title: {reader["Title"]}, Author: {reader["Author"]}, PublicationYear: {reader["PublicationYear"]}");
                        books.Add(new Book
                        {
                            BookID = (int)reader["BookID"],
                            Title = (string)reader["Title"],
                            Author = (string)reader["Author"],
                            PublicationYear = (int)reader["PublicationYear"]
                        });
                    }
                }
            };
            return new OkObjectResult(books);
        }
    }
}
