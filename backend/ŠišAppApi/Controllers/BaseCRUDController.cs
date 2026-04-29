using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;
using ŠišAppApi.Services;
using ŠišAppApi.Services.Interfaces;

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
        protected readonly ICurrentUserService _currentUser;

        public BaseCRUDController(ICRUDService<T, TSearch, TInsert, TUpdate> service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
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

        [HttpDelete("{id}")]
        public virtual async Task<ActionResult<T>> Delete(int id)
        {
            var result = await _service.Delete(id);
            if (result == null)
            {
                 return NotFound();
            }
            return Ok(result);
        }
        protected int GetUserId()
        {
            return _currentUser.UserId ?? 0;
        }

        protected string GetUserRole()
        {
             return _currentUser.Role;
        }
    }
}
