namespace PMTS.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Name { get; set; } //id + file extension
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int ContestantId { get; set; }
        public string TournamentName { get; set; }
        public int TournamentId { get; set; }
    }
}
