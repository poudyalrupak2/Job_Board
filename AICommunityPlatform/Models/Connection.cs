namespace AICommunityPlatform.Models
{
    public enum ConnectionStatus
    {
        Pending,
        Accepted,
        Rejected,
        Following
    }

    public class Connection
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public virtual User Sender { get; set; }

        public int ReceiverId { get; set; }
        public virtual User Receiver { get; set; }

        public ConnectionStatus Status { get; set; } = ConnectionStatus.Pending;

        public DateTime RequestDate { get; set; }

        public DateTime? AcceptedDate { get; set; }
    }
}
