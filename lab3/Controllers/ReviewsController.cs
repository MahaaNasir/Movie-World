using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using lab3.DbData;
using lab3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging; // Added namespace for ILogger

namespace lab3.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ILogger<ReviewsController> _logger; // ILogger instance
        private static DynamoDBContext _context;
        static Connection conn = new Connection();
        private static AmazonDynamoDBClient client = conn.Connect();
        IMovieRepository repository;
        List<Review> reviewList;

        public static Review newReview;
        private IAmazonDynamoDB _dynamoDBClient;

        public ReviewsController(IMovieRepository movieRepo, ILogger<ReviewsController> logger, IAmazonDynamoDB dynamoDBClient)
        {
            repository = movieRepo;
            reviewList = new List<Review>();
            _logger = logger;
            _dynamoDBClient = dynamoDBClient;


            // Establish DynamoDB connection
            var credentials = new BasicAWSCredentials("AKIA5FTZBKIPAEI4I236", "OlL9uD6WBb4vHunUigbqpkWNYhVXsJ1WJ+l5TfaO");
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1
            };
            var client = new AmazonDynamoDBClient(credentials, config);
            _context = new DynamoDBContext(client);
        }




        /// 
        /// Save Reviews
        /// 


        private async Task SaveReviewToDynamoDB(Review review)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDBClient, "Reviews");

                var reviewDocument = new Document();
                reviewDocument["Title"] = review.Title;
                reviewDocument["ReviewDescription"] = review.ReviewDescription;
                reviewDocument["MovieName"] = review.MovieName;
                reviewDocument["MovieRating"] = review.MovieRating;
                reviewDocument["UserEmail"] = review.UserEmail;
                reviewDocument["CreatedAt"] = review.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"); // Convert to ISO 8601 format

                await table.PutItemAsync(reviewDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving review to DynamoDB.");
                throw;
            }
        }


        /// 
        /// Add Reviews
        /// 



        [HttpGet]
        public IActionResult AddReview()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview([Bind("ReviewID,Title,ReviewDescription,MovieName,MovieRating,UserEmail")] Review review)
        {
            if (ModelState.IsValid)
            {
                // Set the CreatedAt property to the current date and time
                review.CreatedAt = DateTime.Now;

                // Save the review to DynamoDB
                await SaveReviewToDynamoDB(review);

                // Redirect to list of reviews page
                return RedirectToAction("GetReviews");
            }
            return View(review);
        }

        /// 
        /// Get the Reviews 
        /// 



        public async Task<IActionResult> GetReviews()
        {
            var reviews = await GetReviewsList();
            return View("ReviewsList", reviews);
        }


        public async Task<List<Review>> GetReviewsList()
        {
            List<Review> reviews = new List<Review>();

            Table reviewsTable = Table.LoadTable(_dynamoDBClient, "Reviews");

            ScanOperationConfig config = new ScanOperationConfig();
            Search search = reviewsTable.Scan(config);

            List<Document> reviewDocuments = await search.GetNextSetAsync();
            foreach (Document reviewDocument in reviewDocuments)
            {
                Review review = new Review
                {
                    Title = reviewDocument["Title"],
                    ReviewDescription = reviewDocument["ReviewDescription"],
                    MovieName = reviewDocument["MovieName"],
                    MovieRating = int.Parse(reviewDocument["MovieRating"]),
                    UserEmail = reviewDocument["UserEmail"],
                };

                DateTime createdAt;
                if (DateTime.TryParse(reviewDocument["CreatedAt"], out createdAt))
                {
                    review.CreatedAt = createdAt;
                }
                else
                {

                    review.CreatedAt = DateTime.MinValue; // Set a default value

                }

                reviews.Add(review);
            }

            return reviews;
        }
        private async Task<Review> GetReviewByTitle(string title)
        {
            // Load the Reviews table
            var table = Table.LoadTable(_dynamoDBClient, "Reviews");

            // Create a query request to retrieve the review with the specified Title
            var request = new QueryRequest
            {
                TableName = "Reviews",
                KeyConditionExpression = "#title = :t",
                ExpressionAttributeNames = new Dictionary<string, string>
        {
            { "#title", "Title" }
        },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":t", new AttributeValue { S = title } }
        }
            };

            // Execute the query request and retrieve the review
            var response = await _dynamoDBClient.QueryAsync(request);

            // Extract the review from the response (assuming there's only one review with the specified Title)
            var reviewDocument = response.Items.FirstOrDefault();
            if (reviewDocument != null)
            {
                // Map the review document to a Review object
                var review = new Review
                {
                    Title = reviewDocument["Title"].S,
                    ReviewDescription = reviewDocument["ReviewDescription"].S,
                    MovieName = reviewDocument["MovieName"].S,
                    MovieRating = int.Parse(reviewDocument["MovieRating"].N), // Assuming MovieRating is numeric
                    UserEmail = reviewDocument["UserEmail"].S,
                    CreatedAt = DateTime.Parse(reviewDocument["CreatedAt"].S) // Assuming CreatedAt is stored as a string in ISO 8601 format
                };

                return review;
            }
            else
            {
                return null; // Review with the specified Title not found
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModifyReview([Bind("Title, ReviewDescription, MovieName, MovieRating, UserEmail, CreatedAt")] Review modifiedReview)
        {
            // Check if the current user's email matches the email associated with the review
            if (modifiedReview.UserEmail == TempData["UserEmail"].ToString())
            {
                // Calculate the time difference between the current time and the review's creation time
                TimeSpan timeDifference = DateTime.Now - modifiedReview.CreatedAt;

                // Check if the time difference is within 24 hours
                if (timeDifference.TotalHours <= 24)
                {
                    // Update the review in DynamoDB
                    await UpdateReviewInDynamoDB(modifiedReview);
                    return RedirectToAction("GetReviews");
                }
                else
                {
                    // Review was not created within the last 24 hours
                    TempData["Error"] = "The review can only be modified within 24 hours of creation.";
                }
            }
            else
            {
                // User is not the owner of the review
                TempData["Error"] = "You are not authorized to modify this review.";
            }

            // Redirect back to the reviews list page
            return RedirectToAction("GetReviews");
        }

        private async Task UpdateReviewInDynamoDB(Review review)
        {
            var table = Table.LoadTable(_dynamoDBClient, "Reviews");

            var reviewDocument = new Document();
            reviewDocument["Title"] = review.Title;
            reviewDocument["ReviewDescription"] = review.ReviewDescription;
            reviewDocument["MovieName"] = review.MovieName;
            reviewDocument["MovieRating"] = review.MovieRating;
            reviewDocument["UserEmail"] = review.UserEmail;
            reviewDocument["CreatedAt"] = review.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"); // Convert to ISO 8601 format

            await table.UpdateItemAsync(reviewDocument);
        }
        [HttpGet]
        public IActionResult EditReview(string title, string reviewDescription, string movieName, int movieRating, string userEmail)
        {
            // Populate the review object with the passed parameters
            var review = new Review
            {
                Title = title,
                ReviewDescription = reviewDescription,
                MovieName = movieName,
                MovieRating = movieRating,
                UserEmail = userEmail
            };

            // Pass the review object to the AddReview view for editing
            return View("AddReview", review);
        }
    }

}