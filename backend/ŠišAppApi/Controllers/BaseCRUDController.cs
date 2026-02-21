using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models; // Adjust namespace
using ŠišAppApi.Services; // Adjust namespace

namespace ŠišAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseCRUDController<T, TSearch, TInsert, TUpdate> : ControllerBase
        where T : class
        where TSearch : class
        where TInsert : class
        where TUpdate : class
    {
        protected readonly ICRUDService<T, TSearch, TInsert, TUpdate> _service;

        public BaseCRUDController(ICRUDService<T, TSearch, TInsert, TUpdate> service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<T>>> Get([FromQuery] TSearch search)
        {
            return Ok(await _service.Get(search));
        }

        [HttpGet("{id}")]
        public virtual async Task<ActionResult<T>> GetById(int id)
        {
            var result = await _service.GetById(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost]
        public virtual async Task<ActionResult<T>> Insert([FromBody] TInsert request)
        {
            var result = await _service.Insert(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public virtual async Task<ActionResult<T>> Update(int id, [FromBody] TUpdate request)
        {
            var result = await _service.Update(id, request);
            return Ok(result);
        }
        protected int GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                 // Fallback or return 0
                 return 0;
            }
            return userId;
        }

        protected string GetUserRole()
        {
             return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        }
    }
}
