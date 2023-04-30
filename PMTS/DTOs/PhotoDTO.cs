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
        [Range(1, 1000, ErrorMessage = "Taškų skaičius turi būti tarp 1 ir 1000.")]
        [Display(Name = "Paukščių skaičius nuotraukoje")]
        public int BirdsN { get; set; }
    }
}
