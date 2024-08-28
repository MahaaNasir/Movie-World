using System.Linq;
using System.Threading.Tasks;
using lab3.Models;

namespace lab3.DbData
{
    public interface IMovieRepository
    {
        IQueryable<Movie> Movies { get; }

        void SaveMovie(Movie movie);
        Movie DeleteMovie(string movieName);
        Task<Movie> GetMovieByName(string movieName); // New method declaration
        Task AddMovieAsync(Movie movie);
    }
}
