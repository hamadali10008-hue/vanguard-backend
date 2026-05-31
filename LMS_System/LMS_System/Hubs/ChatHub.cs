using LMS_System.Data;
using LMS_System.Models;
using Microsoft.AspNetCore.SignalR;

namespace LMS_System.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        // This method is called when a message is sent from React
        public async Task SendMessage(int senderId, int receiverId, string content)
        {
            // 1. Save to Database
            var msg = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.UtcNow // Good practice to add a timestamp
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            // 2. Send to Receiver only
            // We target the group named after the Receiver's ID
            await Clients.Group(receiverId.ToString()).SendAsync("ReceiveMessage", senderId, content);

            // 3. Send back to Sender
            // This ensures the sender's UI also updates with their own message
            await Clients.Group(senderId.ToString()).SendAsync("ReceiveMessage", senderId, content);
        }

        // This method runs automatically when a user connects from React
        public override async Task OnConnectedAsync()
        {
            // We get the 'userId' from the connection string sent by React
            var userId = Context.GetHttpContext()?.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                // Put the user into their own private room
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }
    }
}