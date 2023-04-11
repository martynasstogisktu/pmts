namespace PMTS.Models
{
    public class Contestant
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public string TournamentName { get; set; }
        public int TournamentId { get; set; }
        public int Points { get; set; } = 0;
        public bool Removed { get; set; } = false; //has the user been removed from the tournament
    }
}
