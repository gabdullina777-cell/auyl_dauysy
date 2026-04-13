namespace ayul_dayusy.Models
{
    public class PetitionVote
    {
        public int Id { get; set; }
        public int PetitionId { get; set; }
        public string UserId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}