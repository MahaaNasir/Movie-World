using lab3.DbData;
using lab3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Threading.Tasks;
using System;

namespace lab3.Controllers
{
    public class UsersController : Controller
    {
        private readonly MovieAppDbContext _context;
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private readonly DynamoDBManager _dynamoDBManager;

        public UsersController(MovieAppDbContext context, IAmazonDynamoDB dynamoDBClient, DynamoDBManager dynamoDBManager)
        {
            _context = context;
            _dynamoDBClient = dynamoDBClient;
            _dynamoDBManager = dynamoDBManager;
            _dynamoDBManager = _dynamoDBManager ?? throw new ArgumentNullException(nameof(dynamoDBManager));
        }

        public async Task<IActionResult> Index()
        {
            // Ensure the table exists before proceeding
            await _dynamoDBManager.CreateTableIfNotExistsAsync();

            return View();
        }

        [HttpGet]
        public ViewResult Signin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signin(User userLogin)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userLogin.Email);

            if (user == null)
            {
                TempData["LoginError"] = $"{userLogin.Email} does not exist";
                return View(userLogin);
            }

            // Save user information to DynamoDB
            await _dynamoDBManager.CreateTableIfNotExistsAsync();
            await new SeedData(_dynamoDBClient).InitializeAsync(); // Call InitializeAsync without arguments

            TempData["UserId"] = user.UserId;
            TempData["UserEmail"] = user.Email;
            return RedirectToAction("Index", "Movies");
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup([Bind("UserId,Email,Password,ConfirmPassword")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();

                // Save user information to DynamoDB
                await SaveUserToDynamoDB(user);

                return RedirectToAction("Signin");
            }
            return View(user);
        }

        // Helper method to save user to DynamoDB
        private async Task SaveUserToDynamoDB(User user)
        {
            var table = Table.LoadTable(_dynamoDBClient, "Users");

            var userDocument = new Document();
            userDocument["UserId"] = user.UserId;
            userDocument["Email"] = user.Email;
            userDocument["Password"] = user.Password;

            await table.PutItemAsync(userDocument);
        }
    }
}
