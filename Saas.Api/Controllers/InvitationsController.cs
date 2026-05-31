using Microsoft.AspNetCore.Mvc;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;

namespace Saas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    // 1. POST: Create an invitation (Used by Admin)
    [HttpPost("send")]
    public async Task<IActionResult> SendInvitation([FromBody] InviteRequest request)
    {
        // For now, we manually pass TenantId. 
        // Later, we will get this from the Admin's JWT token.
        var token = await _invitationService.CreateInvitationAsync(
            request.Email,
            request.Role,
            request.TenantId);

        // In a real app, you'd trigger an email service here.
        // For testing, we just return the link.
        var invitationLink = $"https://localhost:3000/signup?token={token}";

        return Ok(new { Message = "Invitation created", Link = invitationLink });
    }

    // 2. GET: Validate a token (Used by the Frontend when the page loads)
    [HttpGet("validate/{token}")]
    public async Task<IActionResult> Validate(string token)
    {
        var invitation = await _invitationService.ValidateInvitationAsync(token);

        if (invitation == null)
            return BadRequest("Invitation is invalid or expired.");

        return Ok(new
        {
            invitation.Email,
            invitation.Role,
            invitation.TenantId
        });
    }
}

// Simple DTO for the request
public record InviteRequest(string Email, string Role, int TenantId);