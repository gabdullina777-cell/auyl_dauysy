namespace ayul_dayusy.Models
{
    public class Petition
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Region { get; set; } = "";
        public string Category { get; set; } = "";
        public int VoteCount { get; set; } = 0;
        public int GoalVotes { get; set; } = 500;
        public bool IsSolved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string AuthorId { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string Village { get; set; } = "";
    }
}