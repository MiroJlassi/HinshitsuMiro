using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HinshitsuService.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config) => _config = config;

        public string GenerateToken(string userId, string email, string username)
        {
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: new[] 
                { 
                    new Claim("id", userId), 
                    new Claim("email", email),
                    new Claim("username", username)
                },
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
