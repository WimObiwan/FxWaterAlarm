using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers;

[Route("api/[controller]")]
public class SensorController : Controller
{
    // GET
    public IActionResult Index()
    {
        object obj = new
        {
            testdata = 5
        };
        return Ok(obj);
    }
}