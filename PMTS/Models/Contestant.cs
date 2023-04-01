namespace PMTS.Models
{
    public class Contestant
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TournamentId { get; set; }
        public int Points { get; set; }
        public bool Removed { get; set; } //has the user been removed from the tournament
    }
}
