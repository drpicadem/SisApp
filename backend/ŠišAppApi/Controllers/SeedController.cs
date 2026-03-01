using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Data;

namespace ŠišAppApi.Controllers;

using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SeedController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SeedController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            DbInitializer.Seed(_context);
            return Ok(new { message = "Baza podataka je uspješno inicijalizirana s testnim podacima." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Greška pri inicijalizaciji baze podataka.", error = ex.Message });
        }
    }
} 