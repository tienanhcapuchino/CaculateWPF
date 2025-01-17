namespace Caculate.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid PayerId { get; set; }
        public long CreatedDate { get; set; } = DateTime.Now.Ticks;
        public double TotalMoney { get; set; }
        public virtual Member Payer { get; set; }
        public virtual ICollection<OrderParticipant> Participants { get; set; }
    }
}
