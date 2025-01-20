using Caculate.Entities;
using System.Collections.ObjectModel;

namespace Caculate
{
    public interface IOrderService
    {
        Task<bool> AddNewOrder(Order order);
        Task<bool> AddOrderParticipants(List<OrderParticipant> participants);
        Task<(List<string> MemberNames, ObservableCollection<DataGridOutstanding> Outstandings, ObservableCollection<DataGridReport> Reports)> GetReportByWeekAsync(DateTime startOfWeek, DateTime endOfWeek, FilterModel? filterModel = null);
    }
}
