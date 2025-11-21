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
    public class TeamStartUpsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TeamStartUpsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/teamstartups
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetTeamStartUps(
            [FromQuery] int? startUpId = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (teamStartUps, total) = await _unitOfWork.TeamStartUps.GetPaginatedAsync(
                page,
                pageSize,
                t => (!startUpId.HasValue || t.StartUpId == startUpId.Value) &&
                     (string.IsNullOrEmpty(userId) || t.UserId == userId) &&
                     (string.IsNullOrEmpty(status) || t.Status == status),
                q => q.OrderByDescending(t => t.Id));

            var teamStartUpDtos = new List<TeamStartUpDto>();
            foreach (var team in teamStartUps)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(team.UserId);
                var startUp = await _unitOfWork.StartUps.GetByIdAsync(team.StartUpId);
                var position = await _unitOfWork.TeamStartUps.FirstOrDefaultAsync(t => t.Id == team.Id);
                var positionEntity = position != null ? await _unitOfWork.TeamStartUps.GetByIdAsync(position.Id) : null;

                teamStartUpDtos.Add(new TeamStartUpDto
                {
                    Id = team.Id,
                    StartUpId = team.StartUpId,
                    StartUpIdea = startUp?.Idea ?? "",
                    UserId = team.UserId,
                    UserFullName = user?.FullName ?? "",
                    UserAvatar = user?.Avatar,
                    PositionId = team.PositionId,
                    PositionName = "", // Will be filled from Position entity
                    Experience = team.Experience,
                    Motivation = team.Motivation,
                    Status = team.Status
                });
            }

            return Ok(new { Data = teamStartUpDtos, Total = total });
        }

        // GET: api/teamstartups/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<TeamStartUpDetailDto>> GetTeamStartUp(int id)
        {
            var teamStartUp = await _unitOfWork.TeamStartUps.GetTeamStartUpWithDetailsAsync(id);

            if (teamStartUp == null)
                return NotFound(new { Message = "Thành viên team không t?n t?i" });

            var teamStartUpDetailDto = new TeamStartUpDetailDto
            {
                Id = teamStartUp.Id,
                StartUpId = teamStartUp.StartUpId,
                UserId = teamStartUp.UserId,
                PositionId = teamStartUp.PositionId,
                Experience = teamStartUp.Experience,
                Motivation = teamStartUp.Motivation,
                Status = teamStartUp.Status,
                User = _mapper.Map<AppUserDto>(teamStartUp.User),
                StartUp = _mapper.Map<StartUpDto>(teamStartUp.StartUp),
                Position = teamStartUp.Position != null ? new PositionDto
                {
                    Id = teamStartUp.Position.Id,
                    Name = teamStartUp.Position.Name
                } : null
            };

            return Ok(teamStartUpDetailDto);
        }

        // GET: api/teamstartups/startup/{startUpId}
        [HttpGet("startup/{startUpId}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetTeamStartUpsByStartUp(int startUpId)
        {
            var teamStartUps = await _unitOfWork.TeamStartUps.GetTeamStartUpsByStartUpIdAsync(startUpId);

            var teamStartUpDtos = teamStartUps.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                PositionId = t.PositionId,
                PositionName = t.Position?.Name ?? "",
                Experience = t.Experience,
                Motivation = t.Motivation,
                Status = t.Status
            }).ToList();

            return Ok(teamStartUpDtos);
        }

        // GET: api/teamstartups/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetTeamStartUpsByUser(string userId)
        {
            var teamStartUps = await _unitOfWork.TeamStartUps.GetTeamStartUpsByUserIdAsync(userId);

            var teamStartUpDtos = teamStartUps.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                PositionId = t.PositionId,
                PositionName = t.Position?.Name ?? "",
                Experience = t.Experience,
                Motivation = t.Motivation,
                Status = t.Status
            }).ToList();

            return Ok(teamStartUpDtos);
        }

        // GET: api/teamstartups/status/{status}
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetTeamStartUpsByStatus(string status)
        {
            var teamStartUps = await _unitOfWork.TeamStartUps.GetTeamStartUpsByStatusAsync(status);

            var teamStartUpDtos = teamStartUps.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                PositionId = t.PositionId,
                PositionName = t.Position?.Name ?? "",
                Experience = t.Experience,
                Motivation = t.Motivation,
                Status = t.Status
            }).ToList();

            return Ok(teamStartUpDtos);
        }

        // POST: api/teamstartups
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<TeamStartUpDto>> CreateTeamStartUp([FromBody] CreateTeamStartUpDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify StartUp exists
            var startUp = await _unitOfWork.StartUps.GetByIdAsync(createDto.StartUpId);
            if (startUp == null)
                return BadRequest(new { Message = "StartUp không t?n t?i" });

            // Verify User exists
            var user = await _unitOfWork.Users.GetByIdAsync(createDto.UserId);
            if (user == null)
                return BadRequest(new { Message = "User không t?n t?i" });

            // Check if user already in this startup
            var existingMember = await _unitOfWork.TeamStartUps.FirstOrDefaultAsync(
                t => t.StartUpId == createDto.StartUpId && t.UserId == createDto.UserId);
            if (existingMember != null)
                return BadRequest(new { Message = "User ?ã tham gia StartUp này r?i" });

            var teamStartUp = _mapper.Map<TeamStartUp>(createDto);
            await _unitOfWork.TeamStartUps.AddAsync(teamStartUp);

            var position = await _unitOfWork.TeamStartUps.GetByIdAsync(teamStartUp.Id);
            var positionEntity = position != null ? await _unitOfWork.TeamStartUps.FirstOrDefaultAsync(t => t.PositionId == position.PositionId) : null;

            var teamStartUpDto = new TeamStartUpDto
            {
                Id = teamStartUp.Id,
                StartUpId = teamStartUp.StartUpId,
                StartUpIdea = startUp.Idea,
                UserId = teamStartUp.UserId,
                UserFullName = user.FullName,
                UserAvatar = user.Avatar,
                PositionId = teamStartUp.PositionId,
                PositionName = "", // Will be filled when Position is loaded
                Experience = teamStartUp.Experience,
                Motivation = teamStartUp.Motivation,
                Status = teamStartUp.Status
            };

            return CreatedAtAction(nameof(GetTeamStartUp), new { id = teamStartUp.Id }, teamStartUpDto);
        }

        // PUT: api/teamstartups/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeamStartUp(int id, [FromBody] UpdateTeamStartUpDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);

            if (teamStartUp == null)
                return NotFound(new { Message = "Thành viên team không t?n t?i" });

            // Only the user who joined or the startup owner can update
            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (teamStartUp.UserId != userId && startUp?.UserId != userId)
                return Forbid();

            _mapper.Map(updateDto, teamStartUp);
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { Message = "C?p nh?t thành viên team thành công" });
        }

        // DELETE: api/teamstartups/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeamStartUp(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);

            if (teamStartUp == null)
                return NotFound(new { Message = "Thành viên team không t?n t?i" });

            // Only the user who joined or the startup owner can delete
            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (teamStartUp.UserId != userId && startUp?.UserId != userId)
                return Forbid();

            await _unitOfWork.TeamStartUps.DeleteAsync(teamStartUp);

            return Ok(new { Message = "Xóa thành viên team thành công" });
        }

        // PUT: api/teamstartups/{id}/status
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTeamStartUpStatus(int id, [FromBody] string status)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);

            if (teamStartUp == null)
                return NotFound(new { Message = "Thành viên team không t?n t?i" });

            // Only the startup owner can update status
            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != userId)
                return Forbid();

            teamStartUp.Status = status;
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { Message = "C?p nh?t tr?ng thái thành công", Status = status });
        }
    }
}
