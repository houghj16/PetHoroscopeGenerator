using Microsoft.AspNetCore.Mvc;

namespace PetHoroscopeGenerator.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LuckyNumbersController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<int>> Get()
    {
        var random = new Random();
        var numbers = Enumerable.Range(1, 6).Select(_ => random.Next(1, 100)).ToArray();
        return Ok(numbers);
    }
}
