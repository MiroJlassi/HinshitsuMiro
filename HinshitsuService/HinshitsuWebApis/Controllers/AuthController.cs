using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using HinshitsuService.Models;
using HinshitsuService.Services;

namespace HinshitsuService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly EmailService _emailService;
        private readonly JwtService _jwtService;

        public AuthController(UserService userService, EmailService emailService, JwtService jwtService)
        {
            _userService = userService;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest("Username is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (await _userService.GetByEmailAsync(request.Email) != null)
                return BadRequest("Email already exists.");

            using var sha256 = SHA256.Create();
            var passwordHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password)));

            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString();

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                VerificationCode = verificationCode,
                IsEmailVerified = false
            };

            await _userService.CreateAsync(user);
            await _emailService.SendVerificationEmailAsync(user.Email, user.VerificationCode!);

            return Ok(new { message = "Signup successful. Check your email to verify.", username = user.Username });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest("Verification code is required.");

            var user = await _userService.GetByVerificationCodeAsync(request.Email, request.Code);
            if (user == null) return BadRequest("Invalid verification code or email.");

            user.IsEmailVerified = true;
            user.VerificationCode = null;
            await _userService.UpdateAsync(user);

            return Ok(new { message = "Email verified successfully.", username = user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null) return BadRequest("Invalid credentials.");

            using var sha256 = SHA256.Create();
            var hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password)));
            if (hash != user.PasswordHash) return BadRequest("Invalid credentials.");
            if (!user.IsEmailVerified) return BadRequest("Email not verified.");

            var token = _jwtService.GenerateToken(user.Id!, user.Email, user.Username);
            return Ok(new { token, username = user.Username, email = user.Email });
        }
    }
}
