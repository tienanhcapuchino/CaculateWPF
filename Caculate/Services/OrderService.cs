using Caculate.DataContext;
using Caculate.Entities;
using NLog;

namespace Caculate
{
    public class OrderService : IOrderService
    {
        private readonly CaculateDbContext _dbContext;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public OrderService(CaculateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> AddNewOrder(Order order)
        {
            try
            {
                await _dbContext.AddAsync(order);
                var result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error occurred while adding new order: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddOrderParticipants(List<OrderParticipant> participants)
        {
            try
            {
                await _dbContext.AddRangeAsync(participants);
                var result = await _dbContext.SaveChangesAsync();
                return result == participants.Count;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error occurred while adding new order participants: {ex.Message}");
                return false;
            }
        }
    }
}
