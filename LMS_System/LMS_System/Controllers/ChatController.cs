using Microsoft.AspNetCore.Http;
using LMS_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("history/{currentUserId}/{targetUserId}")]
        public async Task<IActionResult> GetChatHistory(int currentUserId, int targetUserId)
        {
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == targetUserId) ||
                    (m.SenderId == targetUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp) // Oldest first
                .Select(m => new {
                    senderId = m.SenderId,
                    text = m.Content,
                    timestamp = m.Timestamp
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
