using Caculate.DataContext;

namespace Caculate.Services
{
    public class OrderService
    {
        private readonly CaculateDbContext _dbContext;
        public OrderService(CaculateDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
