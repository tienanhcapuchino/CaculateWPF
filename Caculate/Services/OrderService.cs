using Caculate.DataContext;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Collections.ObjectModel;

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

        public async Task<(List<string> MemberNames, ObservableCollection<DataGridOutstanding> Outstandings, ObservableCollection<DataGridReport> Reports)> GetReportByWeekAsync(DateTime startOfWeek, DateTime endOfWeek, FilterModel? filterModel = null)
        {
            try
            {
                var outstandings = new ObservableCollection<DataGridOutstanding>();
                var reports = new ObservableCollection<DataGridReport>();


                // Fetch orders and related data asynchronously
                var orders = await _dbContext.Orders
                    .Where(x => x.CreatedDate >= startOfWeek.Ticks && x.CreatedDate <= endOfWeek.Ticks)
                    .Include(x => x.Participants)
                    .ThenInclude(x => x.Member)
                    .ToListAsync();

                // Calculate outstanding
                var allMembersJoin = orders
                    .SelectMany(x => x.Participants)
                    .Select(x => new { x.Member.Name, x.Member.Id })
                    .Distinct()
                    .ToList();

                // Populate filter member names
                var memberNames = allMembersJoin.Select(x => x.Name).ToList();

                // Compute outstanding for each member
                foreach (var member in allMembersJoin)
                {
                    var totalMoneyPaid = orders.Where(x => x.PayerId == member.Id).Sum(x => x.TotalMoney);
                    var totalUsed = orders.SelectMany(x => x.Participants).Where(x => x.MemberId == member.Id).Sum(x => x.Money);

                    outstandings.Add(new DataGridOutstanding
                    {
                        Name = member.Name,
                        Outstanding = totalMoneyPaid - totalUsed
                    });
                }

                // Generate weekly report
                foreach (var order in orders)
                {
                    var orderDetails = order.Participants;

                    if (filterModel != null)
                    {
                        if (filterModel.CreatedDate != null)
                        {
                            var filterDate = filterModel.CreatedDate.Value.Date;
                            orderDetails = orderDetails.Where(x => new DateTime(x.CreatedDate).Date == filterDate).ToList();
                        }

                        if (!string.IsNullOrEmpty(filterModel.MemberName) && !filterModel.MemberName.Equals("All"))
                        {
                            orderDetails = orderDetails.Where(x => x.Member.Name == filterModel.MemberName).ToList();
                        }
                    }

                    foreach (var orderDetail in orderDetails)
                    {
                        var dataGridReport = new DataGridReport()
                        {
                            CreatedDate = new DateTime(orderDetail.CreatedDate).ToString("dd/MM/yyyy"),
                            Name = orderDetail.Member.Name,
                            Money = orderDetail.Money
                        };
                        reports.Add(dataGridReport);
                    }

                }

                return (memberNames, outstandings, reports);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when LoadReportByWeek, {ex.Message}");
                throw; // Re-throw exception to let the caller handle it
            }
        }

    }
}
