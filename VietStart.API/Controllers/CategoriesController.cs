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

        public CategoriesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(c => c.DeletedAt == null);
            
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            return Ok(categoryDtos);
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Danh m?c không t?n t?i" });

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return Ok(categoryDto);
        }

        // POST: api/categories
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = new Category
            {
                Name = createDto.Name,
                Description = createDto.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };

            await _unitOfWork.Categories.AddAsync(category);

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }

        // PUT: api/categories/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Danh m?c không t?n t?i" });

            category.Name = updateDto.Name;
            category.Description = updateDto.Description;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.Categories.UpdateAsync(category);

            return Ok(new { Message = "C?p nh?t danh m?c thành công" });
        }

        // DELETE: api/categories/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (category == null)
                return NotFound(new { Message = "Danh m?c không t?n t?i" });

            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _unitOfWork.Categories.UpdateAsync(category);

            return Ok(new { Message = "Xóa danh m?c thành công" });
        }
    }
}
