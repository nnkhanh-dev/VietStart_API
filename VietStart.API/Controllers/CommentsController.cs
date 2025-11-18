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
    public class CommentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CommentsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/comments/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var comments = await _unitOfWork.Comments.GetCommentsByStartupAsync(startupId);

            var commentDtos = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserFullName = c.User.FullName,
                UserAvatar = c.User.Avatar,
                StartUpId = c.StartUpId,
                Content = c.Content,
                ParentCommentId = c.ParentCommentId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Replies = c.Replies.Select(r => new CommentDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserFullName = r.User.FullName,
                    UserAvatar = r.User.Avatar,
                    StartUpId = r.StartUpId,
                    Content = r.Content,
                    ParentCommentId = r.ParentCommentId,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).ToList()
            }).ToList();

            return Ok(commentDtos);
        }

        // GET: api/comments/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<CommentDto>> GetComment(int id)
        {
            var comment = await _unitOfWork.Comments.GetCommentWithRepliesAsync(id);

            if (comment == null)
                return NotFound(new { Message = "Bình luận không tồn tại" });

            var commentDto = new CommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                UserFullName = comment.User.FullName,
                UserAvatar = comment.User.Avatar,
                StartUpId = comment.StartUpId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };

            return Ok(commentDto);
        }

        // POST: api/comments
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId && s.DeletedAt == null);
            if (startup == null)
                return BadRequest(new { Message = "Startup không tồn tại" });

            if (createDto.ParentCommentId.HasValue)
            {
                var parentComment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == createDto.ParentCommentId.Value && c.DeletedAt == null);
                if (parentComment == null)
                    return BadRequest(new { Message = "Bình luận cha không tồn tại" });
            }

            var comment = _mapper.Map<Comment>(createDto);
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;
            comment.CreatedBy = userId;

            await _unitOfWork.Comments.AddAsync(comment);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var commentDto = new CommentDto
            {
                Id = comment.Id,
                UserId = comment.UserId,
                UserFullName = user?.FullName,
                UserAvatar = user?.Avatar,
                StartUpId = comment.StartUpId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };

            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, commentDto);
        }

        // PUT: api/comments/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (comment == null)
                return NotFound(new { Message = "Bình luận không tồn tại" });

            if (comment.UserId != userId)
                return Forbid();

            _mapper.Map(updateDto, comment);
            comment.UpdatedAt = DateTime.UtcNow;
            comment.UpdatedBy = userId;

            await _unitOfWork.Comments.UpdateAsync(comment);

            return Ok(new { Message = "Cập nhật bình luận thành công" });
        }

        // DELETE: api/comments/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var comment = await _unitOfWork.Comments.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (comment == null)
                return NotFound(new { Message = "Bình luận không tồn tại" });

            if (comment.UserId != userId)
                return Forbid();

            comment.DeletedAt = DateTime.UtcNow;
            comment.DeletedBy = userId;

            await _unitOfWork.Comments.UpdateAsync(comment);

            return Ok(new { Message = "Xóa bình luận thành công" });
        }
    }
}
