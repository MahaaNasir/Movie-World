
using lab3.Models;
using System.Linq;

namespace lab3.DbData
{
    public interface IUserRepository
    {
        IQueryable<User> Users { get; }
        void SaveUser(User user);
        User GetUserMovies(string email);

    }
}