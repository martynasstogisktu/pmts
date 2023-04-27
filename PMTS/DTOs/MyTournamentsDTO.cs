using PMTS.Models;

namespace PMTS.DTOs
{
    public class MyTournamentsDTO
    {
        public List<Tournament> organizedTournaments = new List<Tournament>();
        public List<Tournament> memberOfTournaments = new List<Tournament>();
    }
}
