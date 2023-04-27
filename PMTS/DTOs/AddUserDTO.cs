using System.ComponentModel.DataAnnotations;

namespace PMTS.DTOs
{
    public class AddUserDTO
    {
        [Required(ErrorMessage = "Įveskite naudotojo vardą.")]
        public string Name { get; set; }
    }
}
