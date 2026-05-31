using Saas.Domain.Entities;
using static Saas.Domain.Entities.Task;




namespace Saas.Application.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItem>> GetAllTasksAsync();
        Task<TaskItem> CreateTaskAsync(TaskItem task);
    }
}
