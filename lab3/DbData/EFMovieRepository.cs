using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using lab3.Models;
using Microsoft.EntityFrameworkCore;

namespace lab3.DbData
{
    public class EFMovieRepository : IMovieRepository
    {
        private readonly MovieAppDbContext _context;

        public Task AddMovieAsync(Movie movie)
        {
            throw new NotImplementedException();
        }
        public EFMovieRepository(MovieAppDbContext context)
        {
            _context = context;
        }

        public IQueryable<Movie> Movies => _context.Movies;

        public Movie GetMovie(string name)
        {
            return _context.Movies.FirstOrDefault(m => m.MovieName == name);
        }

        public void SaveMovie(Movie movie)
        {
            if (string.IsNullOrEmpty(movie.MovieName))
            {
                _context.Movies.Add(movie);
            }
            else
            {
                Movie dbEntry = _context.Movies.FirstOrDefault(m => m.MovieName == movie.MovieName);
                if (dbEntry != null)
                {
                    dbEntry.Rating = movie.Rating;
                    dbEntry.FilePath = movie.FilePath;
                    dbEntry.ReleaseDate = movie.ReleaseDate;
                    dbEntry.ImageUrl = movie.ImageUrl;
                    dbEntry.Genre = movie.Genre;
                    dbEntry.Description = movie.Description;
                }
                else
                {
                    throw new Exception($"Movie with name '{movie.MovieName}' not found.");
                }
            }
            _context.SaveChanges();
        }

        public Movie DeleteMovie(string movieName)
        {
            Movie dbEntry = _context.Movies.FirstOrDefault(m => m.MovieName == movieName);
            if (dbEntry != null)
            {
                _context.Movies.Remove(dbEntry);
                _context.SaveChanges();
            }
            return dbEntry;
        }

        public async Task<Movie> GetMovieByName(string movieName)
        {
            return await _context.Movies.FirstOrDefaultAsync(m => m.MovieName == movieName);
        }
    }
}