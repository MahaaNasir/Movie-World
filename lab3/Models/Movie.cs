using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace lab3.Models
{
    [DynamoDBTable("Movies")]
    public class Movie
    {
        [DynamoDBHashKey]
        public string MovieName { get; set; }

        [Required]
        public string ReleaseDate { get; set; }

        [Required]
        public string Genre { get; set; } // Change data type to string

        public int Rating { get; set; }

        public string ImageUrl { get; set; }

        public string FilePath { get; set; }

        public string Description { get; set; }
        public string MovieID { get; internal set; }
        public string S3Key { get; set; }

    }
}
