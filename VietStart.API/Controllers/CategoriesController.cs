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
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoriesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/categories
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(c => c.DeletedAt == null);
            
            var categoryDtos = _mapper.Map<IEnumerable<CategoryDto>>(categories);

            return Ok(categoryDtos);
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Danh mục không tồn tại" });

            var categoryDto = _mapper.Map<CategoryDto>(category);

            return Ok(categoryDto);
        }

        // POST: api/categories
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = _mapper.Map<Category>(createDto);
            category.CreatedAt = DateTime.UtcNow;
            category.CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.Categories.AddAsync(category);

            var categoryDto = _mapper.Map<CategoryDto>(category);

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }

        // PUT: api/categories/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Danh mục không tồn tại" });

            _mapper.Map(updateDto, category);
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.Categories.UpdateAsync(category);

            return Ok(new { Message = "Cập nhật danh mục thành công" });
        }

        // DELETE: api/categories/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Danh mục không tồn tại" });

            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.Categories.UpdateAsync(category);

            return Ok(new { Message = "Xóa danh mục thành công" });
        }
    }
}
