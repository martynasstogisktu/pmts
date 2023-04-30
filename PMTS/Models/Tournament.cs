using System.ComponentModel.DataAnnotations;

namespace PMTS.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Įveskite turnyro pavadinimą.")]
        [StringLength(80, ErrorMessage = "Pavadinimas per ilgas.")]
        [MinLength(3, ErrorMessage = "Pavadinimas per trumpas.")]
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public bool RestrictedTypes { get; set; }
        public bool Active { get; set; } = true; //neaktyvus - lyg suarchyvuotas. Kontroliuojamas naudotojo (iki kol turnyras nepasibaigia)
        public bool Ongoing { get; set; } = false; //ar turnyras vyksta (galima jame dalyvauti). Kontroliuojamas sistemos
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Contestant>? Contestants { get; set; }
        public List<Bird>? Birds { get; set; }
        public string? Organizer { get; set; }
        public int UserId { get; set; }
        public int DefaultPoints { get; set; } //kiek tasku verta nuotrauka kai nera rusiu limito

        //public List<Photo> Photos { get; set; }
    }
}
