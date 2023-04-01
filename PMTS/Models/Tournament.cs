namespace PMTS.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public bool RestrictedTypes { get; set; }
        public bool Active { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Contestant>? Contestants { get; set; }
        public List<Bird>? Birds { get; set; }

        //public List<Photo> Photos { get; set; }

    }
}
