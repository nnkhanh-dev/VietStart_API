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
    public class SharesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SharesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/shares/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<IEnumerable<ShareDto>>> GetSharesByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var shares = await _unitOfWork.Shares.GetSharesByStartupAsync(startupId);

            var shareDtos = _mapper.Map<IEnumerable<ShareDto>>(shares);

            return Ok(shareDtos);
        }

        // GET: api/shares/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<IEnumerable<ShareDto>>> GetSharesByUser(string userId)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var shares = await _unitOfWork.Shares.GetSharesByUserAsync(userId);

            var shareDtos = _mapper.Map<IEnumerable<ShareDto>>(shares);

            return Ok(shareDtos);
        }

        // POST: api/shares
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<ShareDto>> CreateShare([FromBody] CreateShareDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId && s.DeletedAt == null);
            if (startup == null)
                return BadRequest(new { Message = "Startup không tồn tại" });

            var existingShare = await _unitOfWork.Shares.GetShareAsync(userId, createDto.StartUpId);
            if (existingShare != null)
                return BadRequest(new { Message = "Bạn đã share startup này rồi" });

            var share = _mapper.Map<Share>(createDto);
            share.UserId = userId;
            share.CreatedAt = DateTime.UtcNow;
            share.CreatedBy = userId;

            await _unitOfWork.Shares.AddAsync(share);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var shareDto = _mapper.Map<ShareDto>(share);
            shareDto.UserFullName = user?.FullName;

            return CreatedAtAction(nameof(GetSharesByStartup), new { startupId = share.StartUpId }, shareDto);
        }

        // PUT: api/shares/{userId}/{startupId}
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{userId}/{startupId}")]
        public async Task<IActionResult> UpdateShare(string userId, int startupId, [FromBody] UpdateShareDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var share = await _unitOfWork.Shares.GetShareAsync(userId, startupId);

            if (share == null)
                return NotFound(new { Message = "Chia sẻ không tồn tại" });

            if (share.UserId != currentUserId)
                return Forbid();

            _mapper.Map(updateDto, share);
            share.UpdatedAt = DateTime.UtcNow;
            share.UpdatedBy = currentUserId;

            await _unitOfWork.Shares.UpdateAsync(share);

            return Ok(new { Message = "Cập nhật chia sẻ thành công" });
        }

        // DELETE: api/shares/{userId}/{startupId}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{userId}/{startupId}")]
        public async Task<IActionResult> DeleteShare(string userId, int startupId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var share = await _unitOfWork.Shares.GetShareAsync(userId, startupId);

            if (share == null)
                return NotFound(new { Message = "Chia sẻ không tồn tại" });

            if (share.UserId != currentUserId)
                return Forbid();

            share.DeletedAt = DateTime.UtcNow;
            share.DeletedBy = currentUserId;

            await _unitOfWork.Shares.UpdateAsync(share);

            return Ok(new { Message = "Xóa chia sẻ thành công" });
        }
    }
}
