using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VietStart.API.Entities.DTO;
using VietStart.API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StartUpMediasController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public StartUpMediasController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/startupmédias/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        public async Task<ActionResult<IEnumerable<StartUpMediaDto>>> GetMediasByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không t?n t?i" });

            var medias = await _unitOfWork.StartUpMedias.GetMediasByStartupAsync(startupId);

            var mediaDtos = medias.Select(m => new StartUpMediaDto
            {
                Id = m.Id,
                Path = m.Path,
                Type = m.Type,
                StartUpId = m.StartUpId
            }).ToList();

            return Ok(mediaDtos);
        }

        // GET: api/startupmédias/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<StartUpMediaDto>> GetMedia(int id)
        {
            var media = await _unitOfWork.StartUpMedias.GetByIdAsync(id);

            if (media == null)
                return NotFound(new { Message = "Media không t?n t?i" });

            var mediaDto = new StartUpMediaDto
            {
                Id = media.Id,
                Path = media.Path,
                Type = media.Type,
                StartUpId = media.StartUpId
            };

            return Ok(mediaDto);
        }

        // POST: api/startupmédias
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<StartUpMediaDto>> CreateMedia([FromBody] CreateStartUpMediaDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId && s.DeletedAt == null);
            if (startup == null)
                return BadRequest(new { Message = "Startup không t?n t?i" });

            if (startup.UserId != userId)
                return Forbid();

            var media = new VietStart.API.Entities.Domains.StartUpMedia
            {
                Path = createDto.Path,
                Type = createDto.Type,
                StartUpId = createDto.StartUpId
            };

            await _unitOfWork.StartUpMedias.AddAsync(media);

            var mediaDto = new StartUpMediaDto
            {
                Id = media.Id,
                Path = media.Path,
                Type = media.Type,
                StartUpId = media.StartUpId
            };

            return CreatedAtAction(nameof(GetMedia), new { id = media.Id }, mediaDto);
        }

        // PUT: api/startupmédias/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMedia(int id, [FromBody] UpdateStartUpMediaDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var media = await _unitOfWork.StartUpMedias.GetByIdAsync(id);

            if (media == null)
                return NotFound(new { Message = "Media không t?n t?i" });

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == media.StartUpId);
            if (startup.UserId != userId)
                return Forbid();

            media.Path = updateDto.Path;
            media.Type = updateDto.Type;

            await _unitOfWork.StartUpMedias.UpdateAsync(media);

            return Ok(new { Message = "C?p nh?t media thành công" });
        }

        // DELETE: api/startupmédias/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var media = await _unitOfWork.StartUpMedias.GetByIdAsync(id);

            if (media == null)
                return NotFound(new { Message = "Media không t?n t?i" });

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == media.StartUpId);
            if (startup.UserId != userId)
                return Forbid();

            await _unitOfWork.StartUpMedias.DeleteAsync(media);

            return Ok(new { Message = "Xóa media thành công" });
        }
    }
}
