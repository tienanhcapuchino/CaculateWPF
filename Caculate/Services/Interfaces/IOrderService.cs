using Caculate.Entities;

namespace Caculate
{
    public interface IOrderService
    {
        Task<bool> AddNewOrder(Order order);
        Task<bool> AddOrderParticipants(List<OrderParticipant> participants);
    }
}
