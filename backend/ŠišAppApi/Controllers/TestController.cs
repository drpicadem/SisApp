using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public ActionResult<TestResponse> GetTest()
    {
        var response = new TestResponse
        {
            Message = "Test uspješan! Swagger radi!",
            Timestamp = DateTime.Now,
            IsSuccess = true
        };

        return Ok(response);
    }
} 