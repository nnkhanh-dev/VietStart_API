using AutoMapper;
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
        private readonly IMapper _mapper;

        public StartUpMediasController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/startupmédias/startup/{startupId}
        [HttpGet("startup/{startupId}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<StartUpMediaDto>>> GetMediasByStartup(int startupId)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == startupId && s.DeletedAt == null);
            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var medias = await _unitOfWork.StartUpMedias.GetMediasByStartupAsync(startupId);

            var mediaDtos = _mapper.Map<IEnumerable<StartUpMediaDto>>(medias);

            return Ok(mediaDtos);
        }

        // GET: api/startupmédias/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<StartUpMediaDto>> GetMedia(int id)
        {
            var media = await _unitOfWork.StartUpMedias.GetByIdAsync(id);

            if (media == null)
                return NotFound(new { Message = "Media không tồn tại" });

            var mediaDto = _mapper.Map<StartUpMediaDto>(media);

            return Ok(mediaDto);
        }

        // POST: api/startupmédias
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<StartUpMediaDto>> CreateMedia([FromBody] CreateStartUpMediaDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == createDto.StartUpId && s.DeletedAt == null);
            if (startup == null)
                return BadRequest(new { Message = "Startup không tồn tại" });

            if (startup.UserId != userId)
                return Forbid();

            var media = _mapper.Map<VietStart.API.Entities.Domains.StartUpMedia>(createDto);

            await _unitOfWork.StartUpMedias.AddAsync(media);

            var mediaDto = _mapper.Map<StartUpMediaDto>(media);

            return CreatedAtAction(nameof(GetMedia), new { id = media.Id }, mediaDto);
        }

        // PUT: api/startupmédias/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMedia(int id, [FromBody] UpdateStartUpMediaDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var media = await _unitOfWork.StartUpMedias.GetByIdAsync(id);

            if (media == null)
                return NotFound(new { Message = "Media không tồn tại" });

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == media.StartUpId);
            if (startup.UserId != userId)
                return Forbid();

            _mapper.Map(updateDto, media);

            await _unitOfWork.StartUpMedias.UpdateAsync(media);

            return Ok(new { Message = "Cập nhật media thành công" });
        }

        // DELETE: api/startupmédias/{id}
        [Authorize(Roles = "Admin,Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var media = await _unitOfWork.StartUpMedias.GetByIdAsync(id);

            if (media == null)
                return NotFound(new { Message = "Media không tồn tại" });

            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == media.StartUpId);
            if (startup.UserId != userId)
                return Forbid();

            await _unitOfWork.StartUpMedias.DeleteAsync(media);

            return Ok(new { Message = "Xóa media thành công" });
        }
    }
}
