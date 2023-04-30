using System.ComponentModel.DataAnnotations;

namespace PMTS.DTOs
{
    public class AddUserDTO
    {
        [Required(ErrorMessage = "Įveskite naudotojo vardą.")]
        [StringLength(20, ErrorMessage = "Naudotojo vardas per ilgas.")]
        [MinLength(3, ErrorMessage = "Naudotojo vardas per trumpas.")]
        public string Name { get; set; }
    }
}
