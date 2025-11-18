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
    public class SharesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public SharesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/shares/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        public async Task<ActionResult<IEnumerable<ShareDto>>> GetSharesByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không t?n t?i" });

            var shares = await _unitOfWork.Shares.GetSharesByStartupAsync(startupId);

            var shareDtos = shares.Select(s => new ShareDto
            {
                UserId = s.UserId,
                UserFullName = s.User.FullName,
                StartUpId = s.StartUpId,
                Content = s.Content,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(shareDtos);
        }

        // GET: api/shares/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ShareDto>>> GetSharesByUser(string userId)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null)
                return NotFound(new { Message = "Ng??i dùng không t?n t?i" });

            var shares = await _unitOfWork.Shares.GetSharesByUserAsync(userId);

            var shareDtos = shares.Select(s => new ShareDto
            {
                UserId = s.UserId,
                UserFullName = s.User.FullName,
                StartUpId = s.StartUpId,
                Content = s.Content,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(shareDtos);
        }

        // POST: api/shares
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ShareDto>> CreateShare([FromBody] CreateShareDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId && s.DeletedAt == null);
            if (startup == null)
                return BadRequest(new { Message = "Startup không t?n t?i" });

            var existingShare = await _unitOfWork.Shares.GetShareAsync(userId, createDto.StartUpId);
            if (existingShare != null)
                return BadRequest(new { Message = "B?n ?ã share startup này r?i" });

            var share = new Share
            {
                UserId = userId,
                StartUpId = createDto.StartUpId,
                Content = createDto.Content,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.Shares.AddAsync(share);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var shareDto = new ShareDto
            {
                UserId = share.UserId,
                UserFullName = user?.FullName,
                StartUpId = share.StartUpId,
                Content = share.Content,
                CreatedAt = share.CreatedAt,
                UpdatedAt = share.UpdatedAt
            };

            return CreatedAtAction(nameof(GetSharesByStartup), new { startupId = share.StartUpId }, shareDto);
        }

        // PUT: api/shares/{userId}/{startupId}
        [Authorize]
        [HttpPut("{userId}/{startupId}")]
        public async Task<IActionResult> UpdateShare(string userId, int startupId, [FromBody] UpdateShareDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var share = await _unitOfWork.Shares.GetShareAsync(userId, startupId);

            if (share == null)
                return NotFound(new { Message = "Chia s? không t?n t?i" });

            if (share.UserId != currentUserId)
                return Forbid();

            share.Content = updateDto.Content;
            share.UpdatedAt = DateTime.UtcNow;
            share.UpdatedBy = currentUserId;

            await _unitOfWork.Shares.UpdateAsync(share);

            return Ok(new { Message = "C?p nh?t chia s? thành công" });
        }

        // DELETE: api/shares/{userId}/{startupId}
        [Authorize]
        [HttpDelete("{userId}/{startupId}")]
        public async Task<IActionResult> DeleteShare(string userId, int startupId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var share = await _unitOfWork.Shares.GetShareAsync(userId, startupId);

            if (share == null)
                return NotFound(new { Message = "Chia s? không t?n t?i" });

            if (share.UserId != currentUserId)
                return Forbid();

            share.DeletedAt = DateTime.UtcNow;
            share.DeletedBy = currentUserId;

            await _unitOfWork.Shares.UpdateAsync(share);

            return Ok(new { Message = "Xóa chia s? thành công" });
        }
    }
}
