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

        public ReactsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/reacts/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        public async Task<ActionResult<IEnumerable<ReactDto>>> GetReactsByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không t?n t?i" });

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
        public async Task<ActionResult<IEnumerable<ReactDto>>> GetReactsByComment(int commentId)
        {
            var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.DeletedAt == null);
            if (comment == null)
                return NotFound(new { Message = "Bình lu?n không t?n t?i" });

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
        public async Task<ActionResult<ReactDto>> GetReact(int id)
        {
            var react = await _unitOfWork.Reacts.GetByIdAsync(id);

            if (react == null)
                return NotFound(new { Message = "Ph?n ?ng không t?n t?i" });

            var reactDto = new ReactDto
            {
                Id = react.Id,
                UserId = react.UserId,
                UserFullName = react.User?.FullName,
                StartUpId = react.StartUpId,
                CommentId = react.CommentId,
                Type = react.Type
            };

            return Ok(reactDto);
        }

        // POST: api/reacts
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReactDto>> CreateReact([FromBody] CreateReactDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!createDto.CommentId.HasValue && !createDto.StartUpId.HasValue)
                return BadRequest(new { Message = "Ph?i ch? ??nh CommentId ho?c StartUpId" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (createDto.StartUpId.HasValue)
            {
                var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId.Value && s.DeletedAt == null);
                if (startup == null)
                    return BadRequest(new { Message = "Startup không t?n t?i" });

                var existingReact = await _unitOfWork.Reacts.GetUserReactOnStartupAsync(userId, createDto.StartUpId.Value);
                if (existingReact != null)
                    return BadRequest(new { Message = "B?n ?ã react bài này r?i" });
            }

            if (createDto.CommentId.HasValue)
            {
                var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == createDto.CommentId.Value && c.DeletedAt == null);
                if (comment == null)
                    return BadRequest(new { Message = "Bình lu?n không t?n t?i" });

                var existingReact = await _unitOfWork.Reacts.GetUserReactOnCommentAsync(userId, createDto.CommentId.Value);
                if (existingReact != null)
                    return BadRequest(new { Message = "B?n ?ã react bình lu?n này r?i" });
            }

            var react = new React
            {
                UserId = userId,
                CommentId = createDto.CommentId,
                StartUpId = createDto.StartUpId,
                Type = createDto.Type
            };

            await _unitOfWork.Reacts.AddAsync(react);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var reactDto = new ReactDto
            {
                Id = react.Id,
                UserId = react.UserId,
                UserFullName = user?.FullName,
                StartUpId = react.StartUpId,
                CommentId = react.CommentId,
                Type = react.Type
            };

            return CreatedAtAction(nameof(GetReact), new { id = react.Id }, reactDto);
        }

        // PUT: api/reacts/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReact(int id, [FromBody] UpdateReactDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var react = await _unitOfWork.Reacts.FirstOrDefaultAsync(r => r.Id == id);

            if (react == null)
                return NotFound(new { Message = "Ph?n ?ng không t?n t?i" });

            if (react.UserId != userId)
                return Forbid();

            react.Type = updateDto.Type;

            await _unitOfWork.Reacts.UpdateAsync(react);

            return Ok(new { Message = "C?p nh?t ph?n ?ng thành công" });
        }

        // DELETE: api/reacts/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReact(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var react = await _unitOfWork.Reacts.FirstOrDefaultAsync(r => r.Id == id);

            if (react == null)
                return NotFound(new { Message = "Ph?n ?ng không t?n t?i" });

            if (react.UserId != userId)
                return Forbid();

            await _unitOfWork.Reacts.DeleteAsync(react);

            return Ok(new { Message = "Xóa ph?n ?ng thành công" });
        }
    }
}
