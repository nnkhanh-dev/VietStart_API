using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VietStart.API.Entities.Domains;
using VietStart.API.Entities.DTO;
using VietStart.API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StartupsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public StartupsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/startups
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetStartups([FromQuery] int? categoryId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (startups, total) = await _unitOfWork.StartUps.GetPaginatedAsync(
                page,
                pageSize,
                s => s.DeletedAt == null && (!categoryId.HasValue || s.CategoryId == categoryId.Value),
                q => q.OrderByDescending(s => s.CreatedAt));

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

            return Ok(new { Data = startupDtos, Total = total });
        }

        // GET: api/startups/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<StartUpDto>> GetStartup(int id)
        {
            var startup = await _unitOfWork.StartUps.GetStartUpWithDetailsAsync(id);

            if (startup == null)
                return NotFound(new { Message = "Startup không t?n t?i" });

            var startupDto = new StartUpDto
            {
                Id = startup.Id,
                Team = startup.Team,
                Idea = startup.Idea,
                Prototype = startup.Prototype,
                Traction = startup.Traction,
                Relationship = startup.Relationship,
                Privacy = startup.Privacy,
                Point = startup.Point,
                UserId = startup.UserId,
                UserFullName = startup.AppUser.FullName,
                CategoryId = startup.CategoryId,
                CategoryName = startup.Category.Name,
                CreatedAt = startup.CreatedAt,
                UpdatedAt = startup.UpdatedAt
            };

            return Ok(startupDto);
        }

        // POST: api/startups
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<StartUpDto>> CreateStartup([FromBody] CreateStartUpDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == createDto.CategoryId && c.DeletedAt == null);
            if (category == null)
                return BadRequest(new { Message = "Danh m?c không t?n t?i" });

            var startup = new StartUp
            {
                Team = createDto.Team,
                Idea = createDto.Idea,
                Prototype = createDto.Prototype,
                Traction = createDto.Traction,
                Relationship = createDto.Relationship,
                Privacy = createDto.Privacy,
                Point = 0,
                UserId = userId,
                CategoryId = createDto.CategoryId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.StartUps.AddAsync(startup);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var startupDto = new StartUpDto
            {
                Id = startup.Id,
                Team = startup.Team,
                Idea = startup.Idea,
                Prototype = startup.Prototype,
                Traction = startup.Traction,
                Relationship = startup.Relationship,
                Privacy = startup.Privacy,
                Point = startup.Point,
                UserId = startup.UserId,
                UserFullName = user?.FullName,
                CategoryId = startup.CategoryId,
                CategoryName = category.Name,
                CreatedAt = startup.CreatedAt,
                UpdatedAt = startup.UpdatedAt
            };

            return CreatedAtAction(nameof(GetStartup), new { id = startup.Id }, startupDto);
        }

        // PUT: api/startups/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStartup(int id, [FromBody] UpdateStartUpDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không t?n t?i" });

            if (startup.UserId != userId)
                return Forbid();

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == updateDto.CategoryId && c.DeletedAt == null);
            if (category == null)
                return BadRequest(new { Message = "Danh m?c không t?n t?i" });

            startup.Team = updateDto.Team;
            startup.Idea = updateDto.Idea;
            startup.Prototype = updateDto.Prototype;
            startup.Traction = updateDto.Traction;
            startup.Relationship = updateDto.Relationship;
            startup.Privacy = updateDto.Privacy;
            startup.CategoryId = updateDto.CategoryId;
            startup.UpdatedAt = DateTime.UtcNow;
            startup.UpdatedBy = userId;

            await _unitOfWork.StartUps.UpdateAsync(startup);

            return Ok(new { Message = "C?p nh?t startup thành công" });
        }

        // DELETE: api/startups/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStartup(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không t?n t?i" });

            if (startup.UserId != userId)
                return Forbid();

            startup.DeletedAt = DateTime.UtcNow;
            startup.DeletedBy = userId;

            await _unitOfWork.StartUps.UpdateAsync(startup);

            return Ok(new { Message = "Xóa startup thành công" });
        }

        // GET: api/startups/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetUserStartups(string userId)
        {
            var startups = await _unitOfWork.StartUps.GetUserStartupsAsync(userId);

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
    }
}
