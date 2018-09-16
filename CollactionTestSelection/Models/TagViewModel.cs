using System.ComponentModel.DataAnnotations;

namespace CollactionTestSelection.Models
{
    public class TagViewModel
    {
        [Required]
        [RegularExpression(@"^([A-Z]+-\d+|master|Friesland)$")] 
        public string Tag { get; set; }
    }
}
