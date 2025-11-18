using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietStart.API.Entities.Domains;
using VietStart.API.Entities.DTO;
using VietStart.API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenReposity _tokenRepository;

        public AuthController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ITokenReposity token)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenRepository = token;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
        {
            var user = new AppUser
            {
                UserName = requestDto.Email,
                Email = requestDto.Email,
                FullName = requestDto.FullName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, requestDto.Password);

            if (result.Succeeded)
            {
                // Kiểm tra role "Client" có tồn tại chưa
                if (!await _roleManager.RoleExistsAsync("Client"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Client"));
                }

                // Gán role cho user
                await _userManager.AddToRoleAsync(user, "Client");
                return Ok(new { Message = "User registered successfully." });
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
        {
            var user = await _userManager.FindByEmailAsync(requestDto.Email);

            if(user != null)
            {
                var result = await _userManager.CheckPasswordAsync(user, requestDto.Password);

                if (result)
                {
                    var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                    var accessToken = await _tokenRepository.CreateJWTToken(user, role);

                    var refreshToken = await _tokenRepository.GenerateRefreshTokenAsync(user);

                    await _tokenRepository.SaveRefreshTokenAsync(refreshToken);

                    var OAuth2Token = new OAuth2Token
                    {
                        access_token = accessToken,
                        refresh_token = refreshToken.Token,
                        token_type = "Bearer",
                        expires_in = 3600,
                        scope = role
                    };

                    return Ok(OAuth2Token);
                }
            }

            return BadRequest(new { Message = "Invalid email or password." });
        }

        [HttpPost]
        [Route("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            var oldToken = await _tokenRepository.GetRefreshTokenAsync(request.Token);

            if (oldToken == null || oldToken.ExpiresAt <= DateTime.UtcNow || oldToken.IsRevoked == true)
                return Unauthorized("Refresh token không hợp lệ hoặc đã hết hạn.");

            // Sinh token mới
            var user = oldToken.User;
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Client";

            var accessToken = await _tokenRepository.CreateJWTToken(user, role);

            // Thu hồi token cũ
            await _tokenRepository.RevokeRefreshTokenAsync(request.Token);

            var refreshToken = await _tokenRepository.GenerateRefreshTokenAsync(user);

            await _tokenRepository.SaveRefreshTokenAsync(refreshToken);


            var OAuth2Token = new OAuth2Token
            {
                access_token = accessToken,
                refresh_token = refreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600,
                scope = role
            };

            return Ok(OAuth2Token);
        }

        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
        {
            await _tokenRepository.RevokeRefreshTokenAsync(request.Token);
            return Ok(new { Message = "Đăng xuất thành công." });
        }

    }
}
