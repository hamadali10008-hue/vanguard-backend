using Microsoft.EntityFrameworkCore;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;
using static Saas.Domain.Entities.Task;



namespace Saas.Appplication.Services
{
    public class TaskService : ITaskService
    
    {
        private readonly IApplicationDbContext _context;
        public TaskService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TaskItem>> GetAllTasksAsync()
        {
            return await _context.Tasks.ToListAsync();
        }

        public async Task<TaskItem> CreateProductAsync(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            throw new NotImplementedException();
        }
    }

}
