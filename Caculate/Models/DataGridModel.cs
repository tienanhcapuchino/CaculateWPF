namespace Caculate
{
    public class DataGridOrderModel
    {
        public Guid MemberId { get; set; }
        public string Name { get; set; }
        public double Money { get; set; } = 0;
        public bool IsPayer { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class DataGridReport
    {
        public string Name { get; set; }
        public double Money { get; set; }
        public string CreatedDate { get; set; }
    }

    public class DataGridOutstanding
    {
        public string Name { get; set; }
        public double Outstanding { get; set; }
    }

}
