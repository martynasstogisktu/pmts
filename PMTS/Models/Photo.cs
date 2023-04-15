﻿namespace PMTS.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "notFound.png"; //id + file extension
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int ContestantId { get; set; }
        public string TournamentName { get; set; }
        public int TournamentId { get; set; }
        public int BirdN { get; set; } //number of birds in photo
        public int Points { get; set; } //number of points assigned to photo
    }
}
