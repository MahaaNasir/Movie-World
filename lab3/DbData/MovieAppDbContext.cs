using lab3.Models;
using Microsoft.EntityFrameworkCore;
using Amazon.DynamoDBv2;
using System;

namespace lab3.DbData
{
    public class MovieAppDbContext : DbContext
    {
        private readonly IAmazonDynamoDB _amazonDynamoDB; // Change the type here
        public DbSet<Movie> Movies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Movie>()
                .HasKey(m => m.MovieName); // Specifies MovieName as the primary key
        }
        public MovieAppDbContext(IAmazonDynamoDB amazonDynamoDB, DbContextOptions<MovieAppDbContext> options)
            : base(options)
        {
            _amazonDynamoDB = amazonDynamoDB ?? throw new ArgumentNullException(nameof(amazonDynamoDB));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configure retry logic
            optionsBuilder.UseSqlServer(
                "Server=database-lab3.c7mqs6wc4lur.us-east-1.rds.amazonaws.com,1433;Database=database-lab3;User Id=admin;Password=adminadmin;",
                options => options.EnableRetryOnFailure());
        }

        // Define your entity DbSet properties here
        public DbSet<User> Users { get; set; }
    }
}
