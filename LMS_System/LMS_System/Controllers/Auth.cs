using LMS_System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMS_System.Models;


namespace LMS_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Auth : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        public Auth(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        { 
            var user = await _context.Userss.FirstOrDefaultAsync(u => u.Username == login.Username);
            // 1. Validate the user (In a real app, check your Database here)
            // For now, let's use your "Hamad" logic
            if (user != null)
            {
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(login.Password, user.Password);
                if (isPasswordValid)
                {
                    var token = GenerateJwtToken(user.Username!);
                    return Ok(new { token });
                    
                }
                isPasswordValid = false;
            }

            return Unauthorized();
        }

        private string GenerateJwtToken(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: [new Claim(ClaimTypes.Name, username)],
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public class LoginModel
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }
    }

    
}
