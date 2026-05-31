using Saas.Domain.Entities;

namespace Saas.Application.Interfaces
{
    public interface IInvitationService
    {
        
            // For the Admin to send an invite
            Task<string> CreateInvitationAsync(string email, string role, int tenantId);

            // For the New User to validate the link they clicked
            Task<UserInvitation?> ValidateInvitationAsync(string token);

        // To mark it as used after they sign up
        System.Threading.Tasks.Task MarkAsUsedAsync(string token);
        }
    }

