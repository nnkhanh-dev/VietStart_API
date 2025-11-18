using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VietStart.API.Entities.Domains;
using VietStart.API.Entities.DTO;
using VietStart.API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public UsersController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AppUserDto>> GetUser(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Ng??i dùng không t?n t?i" });

            var userDto = new AppUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Location = user.Location,
                Bio = user.Bio,
                Avatar = user.Avatar,
                DOB = user.DOB,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(userDto);
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUserDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (users, total) = await _unitOfWork.Users.GetPaginatedAsync(
                page,
                pageSize,
                u => u.DeletedAt == null,
                q => q.OrderBy(u => u.CreatedAt));

            var userDtos = users.Select(u => new AppUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Location = u.Location,
                Bio = u.Bio,
                Avatar = u.Avatar,
                DOB = u.DOB,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).ToList();

            return Ok(new { Data = userDtos, Total = total });
        }

        // PUT: api/users/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateAppUserDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id != currentUserId)
                return Forbid();

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Ng??i dùng không t?n t?i" });

            user.FullName = updateDto.FullName ?? user.FullName;
            user.Location = updateDto.Location ?? user.Location;
            user.Bio = updateDto.Bio ?? user.Bio;
            user.Avatar = updateDto.Avatar ?? user.Avatar;
            user.DOB = updateDto.DOB != DateTime.MinValue ? updateDto.DOB : user.DOB;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = currentUserId;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "C?p nh?t thông tin ng??i dùng thành công" });
        }

        // GET: api/users/{id}/startups
        [HttpGet("{id}/startups")]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetUserStartups(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                return NotFound(new { Message = "Ng??i dùng không t?n t?i" });

            var startups = await _unitOfWork.StartUps.GetUserStartupsAsync(id);

            var startupDtos = startups.Select(s => new StartUpDto
            {
                Id = s.Id,
                Team = s.Team,
                Idea = s.Idea,
                Prototype = s.Prototype,
                Traction = s.Traction,
                Relationship = s.Relationship,
                Privacy = s.Privacy,
                Point = s.Point,
                UserId = s.UserId,
                UserFullName = s.AppUser.FullName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(startupDtos);
        }

        // GET: api/users/search/{keyword}
        [HttpGet("search/{keyword}")]
        public async Task<ActionResult<IEnumerable<AppUserDto>>> SearchUsers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { Message = "T? khóa tìm ki?m không ???c tr?ng" });

            var users = await _unitOfWork.Users.SearchUsersAsync(keyword);

            var userDtos = users.Select(u => new AppUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Location = u.Location,
                Bio = u.Bio,
                Avatar = u.Avatar,
                DOB = u.DOB,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).ToList();

            return Ok(userDtos);
        }

        // GET: api/users/{id}/profile
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<dynamic>> GetUserProfile(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Ng??i dùng không t?n t?i" });

            var startupsCount = await _unitOfWork.StartUps.CountAsync(s => s.UserId == id && s.DeletedAt == null);
            var commentsCount = await _unitOfWork.Comments.CountAsync(c => c.UserId == id && c.DeletedAt == null);
            var sharesCount = await _unitOfWork.Shares.CountAsync(s => s.UserId == id && s.DeletedAt == null);

            return Ok(new
            {
                User = new AppUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Location = user.Location,
                    Bio = user.Bio,
                    Avatar = user.Avatar,
                    DOB = user.DOB,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                },
                Statistics = new
                {
                    StartupsCount = startupsCount,
                    CommentsCount = commentsCount,
                    SharesCount = sharesCount
                }
            });
        }

        // DELETE: api/users/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id != currentUserId)
                return Forbid();

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Ng??i dùng không t?n t?i" });

            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = currentUserId;

            await _unitOfWork.Users.UpdateAsync(user);

            return Ok(new { Message = "Xóa tài kho?n thành công" });
        }
    }
}
