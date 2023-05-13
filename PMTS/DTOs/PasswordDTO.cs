using System.ComponentModel.DataAnnotations;

namespace PMTS.DTOs
{
    public class PasswordDTO
    {
        [Required(ErrorMessage = "Įveskite naują slaptažodį.")]
        [StringLength(72, ErrorMessage = "Slaptažodis per ilgas.")]
        [MinLength(8, ErrorMessage = "Slaptažodis turi būti bent 8 simbolių ilgio.")]
        public string newPassword { get; set; }
        [Required(ErrorMessage = "Pakartokite naują slaptažodį.")]
        [StringLength(72, ErrorMessage = "Slaptažodis per ilgas.")]
        [MinLength(8, ErrorMessage = "Slaptažodis turi būti bent 8 simbolių ilgio.")]
        public string newPasswordConfirm { get; set; }
        [Required(ErrorMessage = "Įveskite seną slaptažodį.")]
        public string oldPassword { get; set; }
    }
}
