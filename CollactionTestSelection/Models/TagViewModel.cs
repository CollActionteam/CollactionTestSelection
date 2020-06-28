using System.ComponentModel.DataAnnotations;

namespace CollactionTestSelection.Models
{
    public class TagViewModel
    {
        [Required]
        public string Tag { get; set; } = null!;
    }
}
