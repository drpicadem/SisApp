using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;

namespace ŠišAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController<T> : ControllerBase where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseController(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // GET: api/[controller]
        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<T>>> GetAll()
        {
            try
            {
                var entities = await _dbSet.ToListAsync();
                return Ok(entities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Greška pri dohvaćanju podataka", error = ex.Message });
            }
        }

        // GET: api/[controller]/5
        [HttpGet("{id}")]
        public virtual async Task<ActionResult<T>> GetById(int id)
        {
            try
            {
                var entity = await _dbSet.FindAsync(id);

                if (entity == null)
                {
                    return NotFound(new { message = $"Entitet s ID-em {id} nije pronađen" });
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Greška pri dohvaćanju podataka", error = ex.Message });
            }
        }

        // POST: api/[controller]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        public virtual async Task<ActionResult<T>> Create(T entity)
        {
            try
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = GetEntityId(entity) }, entity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Greška pri kreiranju entiteta", error = ex.Message });
            }
        }

        // PUT: api/[controller]/5
        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(int id, T entity)
        {
            try
            {
                if (id != GetEntityId(entity))
                {
                    return BadRequest(new { message = "ID u URL-u se ne podudara s ID-em entiteta" });
                }

                _context.Entry(entity).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EntityExists(id))
                    {
                        return NotFound(new { message = $"Entitet s ID-em {id} nije pronađen" });
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Greška pri ažuriranju entiteta", error = ex.Message });
            }
        }

        // DELETE: api/[controller]/5
        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                {
                    return NotFound(new { message = $"Entitet s ID-em {id} nije pronađen" });
                }

                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Greška pri brisanju entiteta", error = ex.Message });
            }
        }

        protected virtual bool EntityExists(int id)
        {
            return _dbSet.Find(id) != null;
        }

        protected virtual int GetEntityId(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException("Entitet nema Id property");
            }

            return (int)idProperty.GetValue(entity);
        }

        protected int GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                 // Fallback or throw? For now return 0 or throw if critical
                 // throw new UnauthorizedAccessException("User ID not found in token");
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