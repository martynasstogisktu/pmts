using System.ComponentModel.DataAnnotations;

namespace PMTS.DTOs
{
    public class PhotoDTO
    {
        [Required(ErrorMessage = "Įkelkite nuotrauką.")]
        [Display(Name = "Nuotrauka")]
        public IFormFile? PhotoData { get; set; }
        public int TournamentId { get; set; }
        [Required(ErrorMessage = "Įveskite paukšių skaičių nuotraukoje.")]
        [Display(Name = "Paukščių skaičius nuotraukoje")]
        public int BirdsN { get; set; }
    }
}
