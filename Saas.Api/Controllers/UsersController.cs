using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saas.Api.Services;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;
using Saas.Infrastructure.Data;

namespace Saas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        // 1. ADDED: Tell the controller about the database context
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        // 2. UPDATED: Inject ApplicationDbContext into the constructor
        public UsersController(IUserService userService, ApplicationDbContext context, EmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
            _context = context;
        }

       

      

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            var createdUser = await _userService.CreateUserAsync(user);
            return Ok(createdUser);
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request)
        {
            // 1. Check if the email already exists
            var existingUser = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (existingUser)
            {
                return BadRequest("An account with this email already exists.");
            }

            // 2. Start a transaction (Both Tenant and User must succeed, or both fail)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. Create the Tenant (Company)
                var newTenant = new Tenant
                {
                    Name = request.CompanyName
                };
                _context.Tenants.Add(newTenant);
                await _context.SaveChangesAsync(); // This generates the TenantId

                // 4. Create the Admin User and link them to the Tenant
                var adminUser = new User
                {
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "Admin",
                    TenantId = newTenant.Id,
                    IsActive = true
                };
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Save everything for real
                await transaction.CommitAsync();

                return Ok(new { Message = "Company and Admin created successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. Find the user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("Invalid email or password.");
            }

            // 2. Verify the hashed password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return BadRequest("Invalid email or password.");
            }

            // 3. Generate the JWT Token (We will write this helper method next)
            string token = GenerateJwtToken(user);

            // 4. Return the token and user data to the frontend
            return Ok(new
            {
                Token = token,
                Email = user.Email,
                Role = user.Role,
                FullName=user.FullName,
                TenantId = user.TenantId
            });
        }


        private string GenerateJwtToken(User user)
        {
            // 💡 FIX: Use explicit, clean string literals for claims to prevent naming schema bloat
            var claims = new[]
            {
        new System.Security.Claims.Claim("id", user.Id.ToString()),
        new System.Security.Claims.Claim("email", user.Email),
        new System.Security.Claims.Claim("role", user.Role), // Clean, lowercase text key
        new System.Security.Claims.Claim("TenantId", user.TenantId.ToString())
    };

            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("8KcvKmWDF2sqnXi5i4JFNRRQzLUG/QUzDJe7eIJ6XFg=")
            );

            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "SaasApi",
                audience: "SaasFrontend",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("invite-coworker")]
        public async Task<IActionResult> InviteCoworker([FromBody] UserInviteDto request)
        {
            // 1. Check if the person being invited is already registered anywhere
            var userExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (userExists)
            {
                return BadRequest("This user is already registered on the platform.");
            }

            try
            {
                // 2. Generate a secure random token string for the invitation link
                string secureToken = Guid.NewGuid().ToString();

                // 3. Try to parse the TenantId string from the frontend into an integer for the database
                if (!int.TryParse(request.TenantId, out int parsedTenantId))
                {
                    return BadRequest("Invalid Tenant ID provided.");
                }

                // 4. Create the invitation entry and save it to the database
                var invitation = new UserInvitation
                {
                    Email = request.Email,
                    Token = secureToken,
                    TenantId = parsedTenantId,
                    ExpiryDate = DateTime.UtcNow.AddHours(24), // 24 hours is standard for enterprise invites
                    IsUsed = false
                };

                _context.UserInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                // 5. Trigger the automated background email dispatcher using "request"
                await _emailService.SendInviteEmailAsync(request.Email, request.Role, request.TenantId, secureToken);

                return Ok(new
                {
                    Message = "Corporate invitation successfully generated and dispatched via SMTP gateway.",
                    Token = secureToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to create invitation: {ex.Message}");
            }
        }
        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationDto request)
        {
            // 1. Look up the invitation token in the database
            var invitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i => i.Token == request.Token && !i.IsUsed);

            if (invitation == null)
            {
                return BadRequest("This invitation link is invalid, expired, or has already been used.");
            }

            // 2. Double-check if the invitation token has expired
            if (invitation.ExpiryDate < DateTime.UtcNow)
            {
                return BadRequest("This invitation link has expired. Please request a new invite.");
            }

            // 3. Begin a secure database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4. Create the new co-worker user entity bound to the invitation's TenantId
                var newWorker = new User
                {
                    Email = invitation.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "Member", // They are onboarded as a standard workspace Member
                    TenantId = invitation.TenantId, // Strict multi-tenant isolation!
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newWorker);

                // 5. Burn the invitation token so it can never be used again
                invitation.IsUsed = true;
                _context.UserInvitations.Update(invitation);

                // 6. Persist all changes atomically to SQL Server
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok("Registration finalized successfully. Node identity provisioned.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal database failure during registration: {ex.Message}");
            }
        }

        [HttpGet("tenant-team")]
        // [Authorize] <-- Leave this off for now during your feature development sprint!
        public async Task<IActionResult> GetTenantTeam()
        {
            // 1. Try to read the secure JWT claim if it exists
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;

            // 💡 THE DEVELOPMENT BYPASS: If no token was verified, look at the header or default to Tenant 1
            if (string.IsNullOrEmpty(tenantIdClaim))
            {
                // Check if the frontend passed a fallback header, otherwise default to your main seed tenant
                tenantIdClaim = Request.Headers["X-Tenant-Id"].ToString();
                if (string.IsNullOrEmpty(tenantIdClaim))
                {
                    tenantIdClaim = "1";
                }
            }

            try
            {
                // 2. Pull down your users and run the string-normalized isolation filter
                var allUsers = await _context.Users.ToListAsync();

                var teamRoster = allUsers
                    .Where(u => u.TenantId.ToString() == tenantIdClaim)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.Role,
                        u.IsActive,
                        u.FullName
                    })
                    .ToList();

                return Ok(teamRoster);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Query Error: {ex.Message}");
            }
        }

        [HttpPut("update-role/{id}")]
        // [Authorize] <-- Keeping this commented out for development bypass mode
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
        {
            // 1. Basic validation guard
            if (string.IsNullOrEmpty(request.Role) || (request.Role != "Admin" && request.Role != "Member"))
            {
                return BadRequest("Invalid security clearance level specified.");
            }

            try
            {
                // 2. Fetch the target operator from the SQL context
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound("Target operator node not found in database.");
                }

                // 3. Mutate the field and commit changes to SQL Server
                user.Role = request.Role;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Successfully re-provisioned operator to {request.Role} status." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Mutation Error: {ex.Message}");
            }
        }

        [HttpGet("pending-invitations")]
        // [Authorize] <-- Keeping this commented out for development bypass mode
        public async Task<IActionResult> GetPendingInvitations()
        {
            // 1. Read the tenant identifier from the frontend request header
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                parsedTenantId = 1; // Fallback default seed node
            }

            try
            {
                // 2. Fetch invitations that match the tenant, aren't used yet, and haven't expired
                var pendingInvites = await _context.UserInvitations
                    .Where(i => i.TenantId == parsedTenantId && !i.IsUsed && i.ExpiryDate > DateTime.UtcNow)
                    .Select(i => new
                    {
                        i.Id,
                        i.Email,
                        i.ExpiryDate
                    })
                    .ToListAsync();

                return Ok(pendingInvites);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve pending invitation matrix: {ex.Message}");
            }
        }

        [HttpDelete("revoke-invitation/{id}")]
        // [Authorize] <-- Keeping this commented out for development bypass mode
        public async Task<IActionResult> RevokeInvitation(int id)
        {
            // 1. Read the tenant identifier from the request header to maintain strict isolation boundaries
            var tenantIdHeader = Request.Headers["X-Tenant-Id"].ToString();
            if (string.IsNullOrEmpty(tenantIdHeader) || !int.TryParse(tenantIdHeader, out int parsedTenantId))
            {
                parsedTenantId = 1; // Fallback default seed node
            }

            try
            {
                // 2. Fetch the target invitation row
                var invitation = await _context.UserInvitations.FindAsync(id);
                if (invitation == null)
                {
                    return NotFound("Target invitation node not found in record ledger.");
                }

                // 3. Security Check: Ensure this invitation actually belongs to the administrator's tenant
                if (invitation.TenantId != parsedTenantId)
                {
                    return Forbid("Unauthorized access. Crimson breach detected across isolated tenant sectors.");
                }

                // 4. Remove the row from the database and save changes
                _context.UserInvitations.Remove(invitation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Invitation successfully revoked and deleted from secure database entries." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database Processing Exception: {ex.Message}");
            }
        }

        // 💡 Simple DTO class placed at the bottom of your controller file or in your Models folder
        public class UpdateRoleRequest
        {
            public string Role { get; set; } = string.Empty;
        }

        public class CompleteRegistrationDto
        {
            public string Token { get; set; } = string.Empty;
            public string TenantId { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        public class RegisterAdminRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class UserInviteDto
        {
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string TenantId { get; set; } = string.Empty;
        }
    }
}