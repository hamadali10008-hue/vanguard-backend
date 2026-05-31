using Saas.Application.Interfaces;
using Saas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Saas.Appplication.Services
{
    public class UserService:IUserService
    {
        private readonly IApplicationDbContext _context;

        public UserService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

    }
}
