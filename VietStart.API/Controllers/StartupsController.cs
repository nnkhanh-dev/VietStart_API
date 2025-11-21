using AutoMapper;
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
        private readonly IMapper _mapper;

        public StartupsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/startups
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
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
                Plan = s.Plan,
                Relationship = s.Relationship,
                Privacy = s.Privacy,
                Point = s.Point,
                IdeaPoint = s.IdeaPoint,
                TeamPoint = s.TeamPoint,
                PrototypePoint = s.PrototypePoint,
                PlanPoint = s.PlanPoint,
                RelationshipPoint = s.RelationshipPoint,
                UserId = s.UserId,
                UserFullName = s.AppUser.FullName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CommentCount = s.Comments?.Count ?? 0,
                ShareCount = s.Shares?.Count ?? 0,
                ReactCount = s.Reacts?.Count ?? 0
            }).ToList();

            return Ok(new { Data = startupDtos, Total = total });
        }

        // GET: api/startups/{id}/details
        [HttpGet("{id}/details")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<StartUpDetailDto>> GetStartupDetails(int id)
        {
            var startup = await _unitOfWork.StartUps.GetStartUpWithDetailsAsync(id);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var startupDetailDto = _mapper.Map<StartUpDetailDto>(startup);

            return Ok(startupDetailDto);
        }

        // GET: api/startups/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<StartUpDto>> GetStartup(int id)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == startup.CategoryId && c.DeletedAt == null);
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == startup.UserId);

            var startupDto = new StartUpDto
            {
                Id = startup.Id,
                Team = startup.Team,
                Idea = startup.Idea,
                Prototype = startup.Prototype,
                Plan = startup.Plan,
                Relationship = startup.Relationship,
                Privacy = startup.Privacy,
                Point = startup.Point,
                IdeaPoint = startup.IdeaPoint,
                TeamPoint = startup.TeamPoint,
                PrototypePoint = startup.PrototypePoint,
                PlanPoint = startup.PlanPoint,
                RelationshipPoint = startup.RelationshipPoint,
                UserId = startup.UserId,
                UserFullName = user?.FullName,
                CategoryId = startup.CategoryId,
                CategoryName = category?.Name,
                CreatedAt = startup.CreatedAt,
                UpdatedAt = startup.UpdatedAt
            };

            return Ok(startupDto);
        }

        // POST: api/startups
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<StartUpDto>> CreateStartup([FromBody] CreateStartUpDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(userId == null)
                return Unauthorized();

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == createDto.CategoryId && c.DeletedAt == null);
            if (category == null)
                return BadRequest(new { Message = "Danh mục không tồn tại" });

            var startup = _mapper.Map<StartUp>(createDto);
            startup.UserId = userId;
            startup.CreatedAt = DateTime.UtcNow;
            startup.CreatedBy = userId;

            await _unitOfWork.StartUps.AddAsync(startup);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var startupDto = new StartUpDto
            {
                Id = startup.Id,
                Team = startup.Team,
                Idea = startup.Idea,
                Prototype = startup.Prototype,
                Plan = startup.Plan,
                Relationship = startup.Relationship,
                Privacy = startup.Privacy,
                Point = startup.Point,
                IdeaPoint = startup.IdeaPoint,
                TeamPoint = startup.TeamPoint,
                PrototypePoint = startup.PrototypePoint,
                PlanPoint = startup.PlanPoint,
                RelationshipPoint = startup.RelationshipPoint,
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
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStartup(int id, [FromBody] UpdateStartUpDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            if (startup.UserId != userId)
                return Forbid();

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == updateDto.CategoryId && c.DeletedAt == null);
            if (category == null)
                return BadRequest(new { Message = "Danh mục không tồn tại" });

            _mapper.Map(updateDto, startup);
            startup.UpdatedAt = DateTime.UtcNow;
            startup.UpdatedBy = userId;

            await _unitOfWork.StartUps.UpdateAsync(startup);

            return Ok(new { Message = "Cập nhật startup thành công" });
        }

        // DELETE: api/startups/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStartup(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

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
                Plan = s.Plan,
                Relationship = s.Relationship,
                Privacy = s.Privacy,
                Point = s.Point,
                IdeaPoint = s.IdeaPoint,
                TeamPoint = s.TeamPoint,
                PrototypePoint = s.PrototypePoint,
                PlanPoint = s.PlanPoint,
                RelationshipPoint = s.RelationshipPoint,
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
