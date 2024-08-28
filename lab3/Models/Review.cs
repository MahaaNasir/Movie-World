using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace lab3.Models
{
    [DynamoDBTable("Reviews")]
    public class Review
    {
        [DynamoDBHashKey]
        public string Title { get; set; }
        public string ReviewDescription { get; set; }
        public string MovieName { get; set; }
        public int MovieRating { get; set; }
        public string UserEmail { get; set; }
        public DateTime CreatedAt { get; set; } // Add Timestamp property
    }
}