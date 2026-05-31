using Microsoft.EntityFrameworkCore;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;


namespace Saas.Appplication.Services

{
    public class ProjectService : IProjectService
    {
        private readonly IApplicationDbContext _context;
        public ProjectService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects.ToListAsync();
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }
    }
}
