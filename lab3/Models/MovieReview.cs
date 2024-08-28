using Microsoft.AspNetCore.Mvc.ViewEngines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace lab3.Models
{
    public class MovieReview
    {
        public Movie Movie { get; set; }
        public Review Review { get; set; }
        public string MovieName { get; internal set; }
    }
}