using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using lab3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lab3.DbData
{
    public class SeedData
    {
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private const string TableName = "Movies";

        public SeedData(IAmazonDynamoDB dynamoDBClient)
        {
            _dynamoDBClient = dynamoDBClient ?? throw new ArgumentNullException(nameof(dynamoDBClient));
        }

        public async Task InitializeAsync()
        {
            try
            {
                if (await TableExistsAsync(TableName))
                {
                    Console.WriteLine("Movies table exists. Seeding data.");
                    // Call method to seed movie data here if needed
                }
                else
                {
                    Console.WriteLine("Movies table does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding data: {ex.Message}");
            }
        }

        private async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var response = await _dynamoDBClient.ListTablesAsync();
                return response.TableNames.Contains(tableName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if table exists: {ex.Message}");
                return false;
            }
        }

        public async Task SeedMoviesAsync(string movieName, string releaseDate, string genre, int rating, string description, string imageUrl)
        {
            try
            {
                // Create a Movie object with the provided input parameters
                var movie = new Movie
                {
                    MovieName = movieName,
                    ReleaseDate = releaseDate,
                    Genre = genre,
                    Rating = rating,
                    Description = description,
                    ImageUrl = imageUrl
                };

                // Call BatchInsertAsync with a list containing only the current movie
                await BatchInsertAsync(new List<Movie> { movie });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding movie data: {ex.Message}");
            }
        }

        private async Task BatchInsertAsync(List<Movie> movies)
        {
            Console.WriteLine($"Inserting {movies.Count} movies into DynamoDB.");

            var batchWriteItems = movies.Select(movie =>
            {
                var item = new Dictionary<string, AttributeValue>
        {
            
            { "Genre", new AttributeValue { S = movie.Genre } },
            { "MovieName", new AttributeValue { S = movie.MovieName } },
            { "Description", new AttributeValue { S = movie.Description } },
            { "ReleaseDate", new AttributeValue { S = movie.ReleaseDate } },
            { "Rating", new AttributeValue { N = movie.Rating.ToString() } },
            { "ImageUrl", new AttributeValue { S = movie.ImageUrl } },
        };
                return new WriteRequest(new PutRequest(item));
            }).ToList();

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
        {
            { TableName, batchWriteItems }
        }
            };

            try
            {
                var response = await _dynamoDBClient.BatchWriteItemAsync(request);

                if (response.UnprocessedItems.Count == 0)
                {
                    Console.WriteLine($"Successfully inserted all movies into DynamoDB.");
                }
                else
                {
                    Console.WriteLine($"Failed to insert some movies into DynamoDB.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting movies into DynamoDB: {ex.Message}");
            }
        }

        // Add other methods for seeding movies if needed
    }
}
