using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using lab3.DbData;
using lab3.Models;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace lab3.Controllers
{
    public class MoviesController : Controller
    {
        private const string V = "lab3bucketmaha";
        private readonly MovieAppDbContext _context;
        private readonly IMovieRepository _movieRepository;
        private readonly ILogger<MoviesController> _logger;
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        public string _bucketName = V;

        public MoviesController(MovieAppDbContext context, IMovieRepository movieRepository, IAmazonDynamoDB dynamoDBClient, ILogger<MoviesController> logger)
        {
            _context = context;
            _movieRepository = movieRepository;
            _dynamoDBClient = dynamoDBClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var movies = await GetMoviesFromDynamoDB();
            return View(movies);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieName,ReleaseDate,Genre,Rating,Description,ImageUrl")] Movie movie, IFormFile file)
        {
            // Check if a file was uploaded
            if (file != null && file.Length > 0)
            {
                // Upload the file to S3 and set the FilePath property
                movie.FilePath = await UploadFileToS3Async(file);
            }

            // Save the movie details to DynamoDB
            await SaveMovieToDynamoDB(movie);

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Details(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return NotFound();
            }

            var movie = await GetMovieFromDynamoDB(name);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        public async Task<IActionResult> DownloadMovie(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await GetMovieFromDynamoDB(id);
            if (movie == null)
            {
                return NotFound();
            }

            var stream = await GetFileFromS3Async(movie.FilePath);
            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, "application/octet-stream", id);
        }

        private async Task<Stream> GetFileFromS3Async(string key)
        {

            var region = Amazon.RegionEndpoint.USEast1;

            // Create AmazonS3Client with the specified region
            var credentials = new BasicAWSCredentials("AKIA5FTZBKIPAEI4I236", "OlL9uD6WBb4vHunUigbqpkWNYhVXsJ1WJ+l5TfaO");
            using (var s3Client = new AmazonS3Client(credentials, Amazon.RegionEndpoint.USEast1))
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                try
                {
                    var response = await s3Client.GetObjectAsync(request);
                    return response.ResponseStream;
                }
                catch (AmazonS3Exception ex)
                {
                    _logger.LogError(ex, "Error downloading file from S3");
                    return null;
                }
            }
        }

        private async Task<string> UploadFileToS3Async(IFormFile file)
        {
            Console.WriteLine("Starting file upload to S3...");

            try
            {
                var credentials = new BasicAWSCredentials("AKIA5FTZBKIPAEI4I236", "OlL9uD6WBb4vHunUigbqpkWNYhVXsJ1WJ+l5TfaO");
                using (var s3Client = new AmazonS3Client(credentials, Amazon.RegionEndpoint.USEast1))
                {
                    var key = Guid.NewGuid().ToString(); // Generate unique key for the file

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);

                        var uploadRequest = new PutObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = key,
                            InputStream = memoryStream,
                            ContentType = file.ContentType
                        };

                        await s3Client.PutObjectAsync(uploadRequest);
                    }

                    Console.WriteLine($"File uploaded successfully. Key: {key}");
                    return key; // Return the S3 key
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file to S3: {ex.Message}");
                throw; // Re-throw the exception to propagate it up the call stack
            }
        }

        private async Task SaveMovieToDynamoDB(Movie movie)
        {
            var table = Table.LoadTable(_dynamoDBClient, "Movies");

            var movieDocument = new Document();
            movieDocument["MovieName"] = movie.MovieName;
            movieDocument["ReleaseDate"] = movie.ReleaseDate;
            movieDocument["Genre"] = movie.Genre;
            movieDocument["Rating"] = movie.Rating;
            movieDocument["Description"] = movie.Description;
            movieDocument["FilePath"] = movie.FilePath;
            movieDocument["ImageUrl"] = movie.ImageUrl;

            await table.PutItemAsync(movieDocument);
        }

        private async Task<Movie> GetMovieFromDynamoDB(string movieName)
        {
            Table moviesTable = Table.LoadTable(_dynamoDBClient, "Movies");

            QueryFilter filter = new QueryFilter("MovieName", QueryOperator.Equal, movieName);
            Search search = moviesTable.Query(filter);

            List<Document> movieDocuments = await search.GetNextSetAsync();
            if (movieDocuments.Count > 0)
            {
                Document movieDocument = movieDocuments.First();
                Movie movie = new Movie
                {
                    MovieName = movieDocument["MovieName"],
                    ReleaseDate = movieDocument["ReleaseDate"],
                    Genre = movieDocument["Genre"],
                    Description = movieDocument["Description"],
                    Rating = int.Parse(movieDocument["Rating"]),
                    FilePath = movieDocument["FilePath"],
                    ImageUrl = movieDocument["ImageUrl"]
                };
                return movie;
            }
            else
            {
                return null;
            }
        }

        private async Task<List<Movie>> GetMoviesFromDynamoDB()
        {
            List<Movie> movies = new List<Movie>();

            Table moviesTable = Table.LoadTable(_dynamoDBClient, "Movies");

            ScanOperationConfig config = new ScanOperationConfig();
            Search search = moviesTable.Scan(config);

            List<Document> movieDocuments = await search.GetNextSetAsync();
            foreach (Document movieDocument in movieDocuments)
            {
                Movie movie = new Movie
                {
                    MovieName = movieDocument["MovieName"],
                    ReleaseDate = movieDocument["ReleaseDate"],
                    Genre = movieDocument["Genre"],
                    Description = movieDocument["Description"],
                    Rating = int.Parse(movieDocument["Rating"]),
                    FilePath = movieDocument["FilePath"],
                    ImageUrl = movieDocument["ImageUrl"]
                };

                movies.Add(movie);
            }

            return movies;
        }



        private async Task UpdateMovieInDynamoDB(Movie movie)
        {
            var table = Table.LoadTable(_dynamoDBClient, "Movies");

            var search = table.Query(new QueryFilter("MovieName", QueryOperator.Equal, movie.MovieName));
            List<Document> movieDocuments = await search.GetNextSetAsync();

            if (movieDocuments.Any())
            {
                var movieDocument = movieDocuments.First();
                movieDocument["ReleaseDate"] = movie.ReleaseDate;
                movieDocument["Genre"] = movie.Genre;
                movieDocument["Rating"] = movie.Rating;
                movieDocument["Description"] = movie.Description;
                movieDocument["FilePath"] = movie.FilePath;
                movieDocument["ImageUrl"] = movie.ImageUrl;

                await table.UpdateItemAsync(movieDocument);
            }
            else
            {
                // Handle the case where the movie with the given name does not exist in DynamoDB
                throw new Exception($"Movie with name '{movie.MovieName}' not found in DynamoDB.");
            }
        }


        private bool MovieExists(string movieName)
        {
            return _context.Movies.Any(e => e.MovieName == movieName);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return NotFound();
            }

            var movie = await GetMovieFromDynamoDB(name);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string name, [Bind("MovieName,ReleaseDate,Genre,Rating,Description,ImageUrl")] Movie movie, IFormFile file)
        {
            if (name != movie.MovieName)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if a new file was uploaded
                    if (file != null && file.Length > 0)
                    {
                        // Upload the new file to S3 and update the FilePath property
                        movie.FilePath = await UploadFileToS3Async(file);
                    }

                    // Update the movie details in DynamoDB
                    await UpdateMovieInDynamoDB(movie);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating the movie.");
                    return View("Error");
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string name)
        {
            if (name == null)
            {
                return NotFound();
            }

            var movie = await GetMovieFromDynamoDB(name);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteMovie(string movieName)
        {
            try
            {
                if (string.IsNullOrEmpty(movieName))
                {
                    return NotFound();
                }

                await DeleteMovieFromDynamoDB(movieName);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the movie.");
                return View("Error");
            }
        }

        private async Task DeleteMovieFromDynamoDB(string movieName)
        {
            var table = Table.LoadTable(_dynamoDBClient, "Movies");

            var search = table.Query(new QueryFilter("MovieName", QueryOperator.Equal, movieName));
            List<Document> movieDocuments = await search.GetNextSetAsync();

            if (movieDocuments.Any())
            {
                var movieDocument = movieDocuments.First();
                await table.DeleteItemAsync(movieDocument);
            }
            else
            {
                throw new Exception($"Movie with name '{movieName}' not found in DynamoDB.");
            }
        }



















        public async Task DeleteItem(string tableName, string movieName)
        {
            var request = new DeleteItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
        {
            {"MovieName", new AttributeValue {S = movieName}}
        }
            };

            await _dynamoDbClient.DeleteItemAsync(request);
        }






        public class ErrorViewModel
        {
            public string RequestId { get; set; }
            public bool ShowRequestId { get; set; }

            public ErrorViewModel(bool showRequestId)
            {
                ShowRequestId = showRequestId;
            }
        }

        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel(true); // Pass true to show the request ID
            errorViewModel.RequestId = HttpContext.TraceIdentifier;
            return View(errorViewModel);
        }

    }
}
