using AutoMapper;
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
        private readonly IMapper _mapper;

        public UsersController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AppUserDto>> GetUser(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var userDto = _mapper.Map<AppUserDto>(user);

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

            var userDtos = _mapper.Map<IEnumerable<AppUserDto>>(users);

            return Ok(new { Data = userDtos, Total = total });
        }

        // PUT: api/users/{id}
        [Authorize(Roles = "Admin,Client")]
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
                return NotFound(new { Message = "Người dùng không tồn tại" });

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

            return Ok(new { Message = "Cập nhật thông tin người dùng thành công" });
        }

        // GET: api/users/{id}/startups
        [HttpGet("{id}/startups")]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetUserStartups(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var startups = await _unitOfWork.StartUps.GetUserStartupsAsync(id);

            var startupDtos = startups.Select(s => new StartUpDto
            {
                Id = s.Id,
                Team = s.Team,
                Idea = s.Idea,
                Prototype = s.Prototype,
                Plan = s.Plan,
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
                return BadRequest(new { Message = "Từ khóa tìm kiếm không được trống" });

            var users = await _unitOfWork.Users.SearchUsersAsync(keyword);

            var userDtos = _mapper.Map<IEnumerable<AppUserDto>>(users);

            return Ok(userDtos);
        }

        // GET: api/users/{id}/profile
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<dynamic>> GetUserProfile(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var startupsCount = await _unitOfWork.StartUps.CountAsync(s => s.UserId == id && s.DeletedAt == null);
            var commentsCount = await _unitOfWork.Comments.CountAsync(c => c.UserId == id && c.DeletedAt == null);
            var sharesCount = await _unitOfWork.Shares.CountAsync(s => s.UserId == id && s.DeletedAt == null);

            return Ok(new
            {
                User = _mapper.Map<AppUserDto>(user),
                Statistics = new
                {
                    StartupsCount = startupsCount,
                    CommentsCount = commentsCount,
                    SharesCount = sharesCount
                }
            });
        }

        // DELETE: api/users/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id != currentUserId)
                return Forbid();

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = currentUserId;

            await _unitOfWork.Users.UpdateAsync(user);

            return Ok(new { Message = "Xóa tài khoản thành công" });
        }
    }
}
