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
    public class ReactsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReactsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/reacts/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<ReactDto>>> GetReactsByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var reacts = await _unitOfWork.Reacts.GetReactsByStartupAsync(startupId);

            var reactDtos = reacts.Select(r => new ReactDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserFullName = r.User.FullName,
                StartUpId = r.StartUpId,
                CommentId = r.CommentId,
                Type = r.Type
            }).ToList();

            return Ok(reactDtos);
        }

        // GET: api/reacts/comment/{commentId}
        [HttpGet("comment/{commentId}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<ReactDto>>> GetReactsByComment(int commentId)
        {
            var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null);
            if (comment == null)
                return NotFound(new { Message = "Bình luận không tồn tại" });

            var reacts = await _unitOfWork.Reacts.GetReactsByCommentAsync(commentId);

            var reactDtos = reacts.Select(r => new ReactDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserFullName = r.User.FullName,
                StartUpId = r.StartUpId,
                CommentId = r.CommentId,
                Type = r.Type
            }).ToList();

            return Ok(reactDtos);
        }

        // GET: api/reacts/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<ReactDto>> GetReact(int id)
        {
            var react = await _unitOfWork.Reacts.GetByIdAsync(id);

            if (react == null)
                return NotFound(new { Message = "Phản ứng không tồn tại" });

            var reactDto = _mapper.Map<ReactDto>(react);
            reactDto.UserFullName = react.User?.FullName;

            return Ok(reactDto);
        }

        // POST: api/reacts
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<ReactDto>> CreateReact([FromBody] CreateReactDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!createDto.CommentId.HasValue && !createDto.StartUpId.HasValue)
                return BadRequest(new { Message = "Phải chỉ định CommentId hoặc StartUpId" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (createDto.StartUpId.HasValue)
            {
                var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId.Value && s.DeletedAt == null);
                if (startup == null)
                    return BadRequest(new { Message = "Startup không tồn tại" });

                var existingReact = await _unitOfWork.Reacts.GetUserReactOnStartupAsync(userId, createDto.StartUpId.Value);
                if (existingReact != null)
                    return BadRequest(new { Message = "Bạn đã react bài này rồi" });
            }

            if (createDto.CommentId.HasValue)
            {
                var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == createDto.CommentId.Value && c.DeletedAt == null);
                if (comment == null)
                    return BadRequest(new { Message = "Bình luận không tồn tại" });

                var existingReact = await _unitOfWork.Reacts.GetUserReactOnCommentAsync(userId, createDto.CommentId.Value);
                if (existingReact != null)
                    return BadRequest(new { Message = "Bạn đã react bình luận này rồi" });
            }

            var react = _mapper.Map<React>(createDto);
            react.UserId = userId;

            await _unitOfWork.Reacts.AddAsync(react);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var reactDto = _mapper.Map<ReactDto>(react);
            reactDto.UserFullName = user?.FullName;

            return CreatedAtAction(nameof(GetReact), new { id = react.Id }, reactDto);
        }

        // PUT: api/reacts/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReact(int id, [FromBody] UpdateReactDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var react = await _unitOfWork.Reacts.FirstOrDefaultAsync(r => r.Id == id);

            if (react == null)
                return NotFound(new { Message = "Phản ứng không tồn tại" });

            if (react.UserId != userId)
                return Forbid();

            _mapper.Map(updateDto, react);

            await _unitOfWork.Reacts.UpdateAsync(react);

            return Ok(new { Message = "Cập nhật phản ứng thành công" });
        }

        // DELETE: api/reacts/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReact(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var react = await _unitOfWork.Reacts.FirstOrDefaultAsync(r => r.Id == id);

            if (react == null)
                return NotFound(new { Message = "Phản ứng không tồn tại" });

            if (react.UserId != userId)
                return Forbid();

            await _unitOfWork.Reacts.DeleteAsync(react);

            return Ok(new { Message = "Xóa phản ứng thành công" });
        }
    }
}
