namespace Caculate.Entities
{
    public class OrderParticipant
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public double Money { get; set; }
        public long CreatedDate { get; set; } = DateTime.Now.Ticks;
        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; }
        public virtual Member Member { get; set; }
    }
}
