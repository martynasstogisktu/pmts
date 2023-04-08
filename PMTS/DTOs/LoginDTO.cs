using System.ComponentModel.DataAnnotations;

namespace PMTS.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Įveskite naudotojo vardą.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Įveskite slaptažodį.")]
        public string Password { get; set; }
    }
}
