using System.ComponentModel;

namespace Caculate
{
    public class DataGridOrderModel : INotifyPropertyChanged
    {
        private DateTime createdDate = DateTime.Now;
        public Guid MemberId { get; set; }
        public string Name { get; set; }
        public double Money { get; set; } = 0;
        public bool IsPayer { get; set; } = false;
        public DateTime CreatedDate
        {
            get => createdDate;
            set
            {
                if (createdDate != value)
                {
                    createdDate = value;
                    OnPropertyChanged(nameof(CreatedDate));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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

    public class FilterModel
    {
        public DateTime? CreatedDate { get; set; } = null;
        public string? MemberName { get; set; } = "";
    }

}
