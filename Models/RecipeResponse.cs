using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RecipeApi.Models
{
    public class RecipeResponse
    {
        public string? RecipeName { get; set; }
        public List<string>? Ingredients { get; set; }
        public List<string>? Instructions { get; set; }
        
        // Assuming SpecialNotes can be both a string or a list
        // public List<string>? SpecialNotes { get; set; }
        
        // public NutritionalInformation? NutritionalInformation { get; set; }
    }

    public class NutritionalInformation
    {
        public int? Calories { get; set; }

        [JsonPropertyName("Protein")]
        public int? Protein { get; set; }

        [JsonPropertyName("Carbs")]
        public int? Carbohydrates { get; set; }

        public int? Fat { get; set; }
        public int? Fiber { get; set; }

        // Include other nutritional fields if they are part of the JSON response
    }
}
