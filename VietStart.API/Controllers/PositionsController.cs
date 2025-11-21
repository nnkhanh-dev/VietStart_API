using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietStart.API.Entities.Domains;
using VietStart.API.Entities.DTO;
using VietStart.API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PositionsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/positions
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositions([FromQuery] string? keyword = null)
        {
            IEnumerable<Position> positions;

            if (!string.IsNullOrEmpty(keyword))
            {
                positions = await _unitOfWork.Positions.SearchPositionsAsync(keyword);
            }
            else
            {
                positions = await _unitOfWork.Positions.GetAllAsync();
            }

            var positionDtos = _mapper.Map<IEnumerable<PositionDto>>(positions);
            return Ok(positionDtos);
        }

        // GET: api/positions/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<PositionDto>> GetPosition(int id)
        {
            var position = await _unitOfWork.Positions.GetByIdAsync(id);

            if (position == null)
                return NotFound(new { Message = "Position không t?n t?i" });

            var positionDto = _mapper.Map<PositionDto>(position);
            return Ok(positionDto);
        }

        // GET: api/positions/name/{name}
        [HttpGet("name/{name}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<PositionDto>> GetPositionByName(string name)
        {
            var position = await _unitOfWork.Positions.GetPositionByNameAsync(name);

            if (position == null)
                return NotFound(new { Message = "Position không t?n t?i" });

            var positionDto = _mapper.Map<PositionDto>(position);
            return Ok(positionDto);
        }

        // POST: api/positions
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<PositionDto>> CreatePosition([FromBody] CreatePositionDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if position with same name exists
            var existingPosition = await _unitOfWork.Positions.GetPositionByNameAsync(createDto.Name);
            if (existingPosition != null)
                return BadRequest(new { Message = "Position v?i tên này ?ã t?n t?i" });

            var position = _mapper.Map<Position>(createDto);
            await _unitOfWork.Positions.AddAsync(position);

            var positionDto = _mapper.Map<PositionDto>(position);
            return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, positionDto);
        }

        // PUT: api/positions/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var position = await _unitOfWork.Positions.GetByIdAsync(id);

            if (position == null)
                return NotFound(new { Message = "Position không t?n t?i" });

            // Check if another position with same name exists
            var existingPosition = await _unitOfWork.Positions.GetPositionByNameAsync(updateDto.Name);
            if (existingPosition != null && existingPosition.Id != id)
                return BadRequest(new { Message = "Position v?i tên này ?ã t?n t?i" });

            _mapper.Map(updateDto, position);
            await _unitOfWork.Positions.UpdateAsync(position);

            return Ok(new { Message = "C?p nh?t position thành công" });
        }

        // DELETE: api/positions/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            var position = await _unitOfWork.Positions.GetByIdAsync(id);

            if (position == null)
                return NotFound(new { Message = "Position không t?n t?i" });

            // Check if position is being used in TeamStartUp
            var isUsed = await _unitOfWork.TeamStartUps.AnyAsync(t => t.PositionId == id);
            if (isUsed)
                return BadRequest(new { Message = "Không th? xóa Position ?ang ???c s? d?ng" });

            await _unitOfWork.Positions.DeleteAsync(position);

            return Ok(new { Message = "Xóa position thành công" });
        }
    }
}
