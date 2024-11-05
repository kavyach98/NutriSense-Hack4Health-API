using Microsoft.AspNetCore.Mvc;
using RecipeApi.Models;
using RecipeApi.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecipeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipeController : ControllerBase
    {
        private readonly OpenAiService _openAiService;

        public RecipeController(OpenAiService openAiService)
        {
            _openAiService = openAiService;
        }

        [HttpPost("get-recipes")]
        public async Task<IActionResult> GetRecipes([FromBody] RecipeRequest request)
        {
            if (string.IsNullOrEmpty(request.BloodGlucoseLevel))
            {
                return BadRequest("BloodGlucoseLevel is required.");
            }

            var recipes = await _openAiService.GetRecipesAsync(request.BloodGlucoseLevel);
            return Ok(recipes);
        }
    }
}
