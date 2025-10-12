using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ITokenReposity _token;

        public AuthController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ITokenReposity token)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _token = token;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
        {
            var user = new AppUser
            {
                UserName = requestDto.Email,
                Email = requestDto.Email,
                FullName = requestDto.FullName
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

                    var OAuth2Token = new OAuth2Token
                    {
                        access_token = await _token.CreateJWTToken(user, role),
                        refresh_token = "temp",
                        token_type = "Bearer",
                        expires_in = 3600,
                        scope = role
                    };

                    return Ok(OAuth2Token);
                }
            }

            return BadRequest(new { Message = "Invalid email or password." });
        }

        [Authorize(Roles = "Client")]
        [HttpGet]
        [Route("Test")]
        public async Task<IActionResult> Test()
        {
            return Ok();
        }

    }
}
