using Microsoft.EntityFrameworkCore;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;

namespace Saas.Appplication.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IApplicationDbContext _context;

        public InvitationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateInvitationAsync(string email, string role, int tenantId)
        {
            var invitation = new UserInvitation
            {
                Email = email,
                Role = role,
                TenantId = tenantId,
                Token = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsUsed = false
            };

            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            return invitation.Token;
        }

        public async Task<UserInvitation?> ValidateInvitationAsync(string token)
        {
            return await _context.UserInvitations
                .FirstOrDefaultAsync(x => x.Token == token
                                     && !x.IsUsed
                                     && x.ExpiryDate > DateTime.UtcNow);
        }

        public async System.Threading.Tasks.Task MarkAsUsedAsync(string token)
        {
            var invite = await _context.UserInvitations.FirstOrDefaultAsync(x => x.Token == token);
            if (invite != null)
            {
                invite.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
