using Saas.Domain.Entities;

namespace Saas.Application.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<Project> CreateProjectAsync(Project project);
    }
}
