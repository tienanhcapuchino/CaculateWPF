using Caculate.DataContext;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace Caculate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IMemberService _memberService;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MainWindow(IMemberService memberService,
            IOrderService orderService)
        {
            InitializeComponent();
            dtgOrders.ItemsSource = dataGridOrderModels;
            dtgReport.ItemsSource = dataGridReports;
            dtgOutstanding.ItemsSource = dataGridOutstandings;
            _memberService = memberService;
            _orderService = orderService;
            LoadData();
        }

        private List<string> MembersFilter = ["All"];
        private ObservableCollection<DataGridOrderModel> dataGridOrderModels = [];
        private ObservableCollection<DataGridReport> dataGridReports = [];
        private ObservableCollection<DataGridOutstanding> dataGridOutstandings = [];

        private Uri? GetLoadingSpinnerUri()
        {
            var gifFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "loading_spinner.gif");
            var gifUri = new Uri(gifFilePath);
            return gifUri;
        }

        private void LoadData()
        {
            var giftUri = GetLoadingSpinnerUri();
            ImageBehavior.SetAnimatedSource(loadingSpinnerGift, new BitmapImage(giftUri));
            loadingSpinnerGift.Visibility = Visibility.Visible;
            //Task.Delay(5000);
            var (startOfWeek, endOfWeek) = GetCurrentWeek();
            tbWeek.Text = $"TUẦN: {startOfWeek:dd/MM/yyyy} - {endOfWeek:dd/MM/yyyy}";
            var members = _memberService.GetAllMembers().GetAwaiter().GetResult();

            members.ForEach(member =>
            {
                dataGridOrderModels.Add(new DataGridOrderModel()
                {
                    MemberId = member.Id,
                    Name = member.Name
                });
            });

            LoadReportByWeek();
            //loadingSpinnerGift.Visibility = Visibility.Collapsed;
        }


        private void LoadReportByWeek(FilterModel? filterModel = null)
        {
            try
            {
                dataGridReports.Clear();
                dataGridOutstandings.Clear();
                var (startOfWeek, endOfWeek) = GetCurrentWeek();
                using (var context = new CaculateDbContext())
                {
                    var orders = context.Orders
                        .Where(x => x.CreatedDate >= startOfWeek.Ticks && x.CreatedDate <= endOfWeek.Ticks)
                        .Include(x => x.Participants)
                        .ThenInclude(x => x.Member)
                        .ToList();

                    //caculate outstanding
                    var allMembersJoin = orders.SelectMany(x => x.Participants).Select(x => new
                    {
                        x.Member.Name,
                        x.Member.Id
                    }).Distinct().ToList();

                    if (filterModel == null)
                    {
                        //load combobox filter members
                        MembersFilter.AddRange(allMembersJoin.Select(x => x.Name).ToList());
                        cbFilterMembers.ItemsSource = MembersFilter;
                    }

                    foreach (var member in allMembersJoin)
                    {
                        var totalMoneyPaid = orders.Where(x => x.PayerId == member.Id).Sum(x => x.TotalMoney);
                        var totalUsed = orders.SelectMany(x => x.Participants).Where(x => x.MemberId == member.Id).Sum(x => x.Money);
                        var outstanding = new DataGridOutstanding()
                        {
                            Name = member.Name,
                            Outstanding = totalMoneyPaid - totalUsed
                        };
                        dataGridOutstandings.Add(outstanding);
                    }

                    //weekly report
                    if (orders?.Count > 0)
                    {
                        foreach (var order in orders)
                        {
                            var orderDetails = order.Participants;
                            if (orderDetails?.Count > 0)
                            {
                                if (filterModel != null)
                                {
                                    if (filterModel.CreatedDate != null)
                                    {
                                        orderDetails = orderDetails.Where(x => new DateTime(x.CreatedDate).Date == filterModel.CreatedDate.Value.Date).ToList();
                                    }
                                    if (!string.IsNullOrEmpty(filterModel.MemberName) && !filterModel.MemberName.Equals("All"))
                                    {
                                        orderDetails = orderDetails.Where(x => x.Member.Name == filterModel.MemberName).ToList();
                                    }
                                }
                                foreach (var orderDetail in orderDetails)
                                {
                                    var reportDetail = new DataGridReport()
                                    {
                                        CreatedDate = (new DateTime(orderDetail.CreatedDate)).ToString("dd/MM/yyyy"),
                                        Name = orderDetail.Member.Name,
                                        Money = orderDetail.Money
                                    };
                                    dataGridReports.Add(reportDetail);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }


        private (DateTime StartOfWeek, DateTime EndOfWeek) GetCurrentWeek()
        {
            DateTime today = DateTime.Today;
            DayOfWeek currentDay = today.DayOfWeek;
            int daysToSubtract = (int)currentDay - (int)DayOfWeek.Monday;
            DateTime startOfWeek = today.AddDays(-daysToSubtract);
            DateTime endOfWeek = startOfWeek.AddDays(6);
            return (startOfWeek, endOfWeek);
        }

        private void tbSubmitOrder_Click(object sender, RoutedEventArgs e)
        {
            //validate
            if (dataGridOrderModels.Count == 0)
            {
                MessageBox.Show("Hãy thêm thông tin đơn hàng trước!");
                return;
            }
            var countIsPayer = dataGridOrderModels.Count(x => x.IsPayer);

            if (countIsPayer > 1)
            {
                MessageBox.Show("Bạn chỉ có thể chọn một người trả!");
                return;
            }

            if (countIsPayer == 0)
            {
                MessageBox.Show("Hãy chọn người trả!");
                return;
            }

            try
            {
                var totalMoney = dataGridOrderModels.Sum(x => x.Money);
                var payerName = dataGridOrderModels.FirstOrDefault(x => x.IsPayer)?.Name?.Trim();
                if (string.IsNullOrEmpty(payerName))
                {
                    MessageBox.Show("Không có người trả!");
                    return;
                }
                var payer = _memberService.GetMemberByName(payerName).GetAwaiter().GetResult();
                if (payer == null)
                {
                    MessageBox.Show("Người trả không tồn tại trong hệ thống!");
                    return;
                }

                var order = new Order()
                {
                    Id = Guid.NewGuid(),
                    CreatedDate = DateTime.Now.Ticks,
                    TotalMoney = totalMoney,
                    PayerId = payer.Id
                };
                var resultAddOrder = _orderService.AddNewOrder(order).GetAwaiter().GetResult();
                if (!resultAddOrder)
                {
                    MessageBox.Show("Đã có lỗi khi lưu đơn hàng!");
                    return;
                }

                List<OrderParticipant> orderParticipants = [];
                foreach (var item in dataGridOrderModels)
                {
                    var member = _memberService.GetMemberById(item.MemberId).GetAwaiter().GetResult();
                    if (member == null)
                    {
                        MessageBox.Show($"Thành viên: {item.Name} không tồn tại trong hệ thống! Hãy thêm thành viên trước!");
                        return;
                    }
                    var orderParticipant = new OrderParticipant()
                    {
                        Id = Guid.NewGuid(),
                        MemberId = member.Id,
                        Money = item.Money,
                        CreatedDate = item.CreatedDate.Ticks,
                        OrderId = order.Id
                    };
                    orderParticipants.Add(orderParticipant);
                }
                if (orderParticipants.Count > 0)
                {
                    var resultAdd = _orderService.AddOrderParticipants(orderParticipants).GetAwaiter().GetResult();
                    if (!resultAdd)
                    {
                        MessageBox.Show("Đã có lỗi khi lưu thông tin đơn hàng!");
                        return;
                    }
                }
                MessageBox.Show("Lưu đơn hàng thành công!");
                dataGridOrderModels.Clear();
                btRefresh_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btRefresh_Click(object sender, RoutedEventArgs e)
        {
            dataGridOrderModels.Clear();
            dataGridReports.Clear();
            dataGridOutstandings.Clear();
            LoadData();
            //AppHelper.ShowPopupMessage("Refresh data successfully", PopupMessageType.Info, DialogHostExample).GetAwaiter().GetResult();
        }

        private void SubmitNameDialogButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbNewMember.Text))
            {
                MessageBox.Show("Hãy nhập tên thành viên!");
                return;
            }
            try
            {
                var member = new Member()
                {
                    Id = Guid.NewGuid(),
                    Name = tbNewMember.Text.Trim()
                };
                var isExistName = _memberService.IsExistName(member.Name).GetAwaiter().GetResult();
                if (isExistName)
                {
                    MessageBox.Show("Thành viên đã tồn tại trong hệ thống!");
                    return;
                }

                var resultAdd = _memberService.AddNewMember(member).GetAwaiter().GetResult();
                if (!resultAdd)
                {
                    MessageBox.Show("Đã có lỗi khi thêm!");
                }
                AddedMemberDialog.IsOpen = false;
                MessageBox.Show("Thêm thành viên thành công!");
                btRefresh_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void CloseNameDialogButton_Click(object sender, RoutedEventArgs e)
        {
            AddedMemberDialog.IsOpen = false;
        }

        private void AddNewMember_Click(object sender, RoutedEventArgs e)
        {
            AddedMemberDialog.IsOpen = true;
        }

        private void EditMember_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dtgOrders.CommitEdit(DataGridEditingUnit.Row, true) && dtgOrders.CommitEdit())
                {
                    var button = sender as Button;
                    DataGridOrderModel? member = button?.DataContext as DataGridOrderModel;
                    if (member != null)
                    {
                        var memberEntity = _memberService.GetMemberById(member.MemberId).GetAwaiter().GetResult();
                        if (memberEntity != null && !memberEntity.Equals(member.Name.Trim()))
                        {
                            memberEntity.Name = member.Name.Trim();
                            var result = _memberService.UpdateMember(memberEntity).GetAwaiter().GetResult();
                            if (!result)
                            {
                                MessageBox.Show($"Thành viên: {member.Name} đã tồn tại trong hệ thống!");
                                return;
                            }
                            MessageBox.Show($"Cập nhật thành viên: {member.Name} thành công!");
                            btRefresh_Click(sender, e);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when edit member: {ex.Message}");
                return;
            }
        }

        private void DeleteMember_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var member = button?.DataContext as DataGridOrderModel;
                if (member != null)
                {
                    DeleteMemberAsync(member.MemberId);
                    btRefresh_Click(sender, e);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when delete member: {ex.Message}");
                return;
            }
        }

        private void DeleteMemberAsync(Guid memberId)
        {
            try
            {
                var resultDelete = _memberService.DeleteMember(memberId).GetAwaiter().GetResult();
                if (resultDelete)
                {
                    MessageBox.Show("Đã xóa thành viên!");
                }
                else
                {
                    MessageBox.Show("Đã có lỗi khi xóa thành viên!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when delete member: {ex.Message}");
                return;
            }
        }

        private void btUpdateOrderDateAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedOrderDate = dtpOrderDateCustom.SelectedDate;
                if (!selectedOrderDate.HasValue)
                {
                    MessageBox.Show("Hãy chọn ngày trước!");
                    return;
                }

                foreach (var item in dataGridOrderModels)
                {
                    item.CreatedDate = selectedOrderDate.Value;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when update date order all, {ex.Message}");
                return;
            }
        }

        private FilterModel _filterModel;

        private void cbFilterMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var currentMemberFilter = cbFilterMembers.SelectedItem.ToString();
            var selectedDate = dtpFilterDate.SelectedDate;
            if (!string.IsNullOrEmpty(currentMemberFilter))
            {
                var filterModel = new FilterModel()
                {
                    CreatedDate = selectedDate != null ? selectedDate.Value : null,
                    MemberName = currentMemberFilter
                };
                LoadReportByWeek(filterModel);
            }
        }

        private void dtpFilterDate_CalendarClosed(object sender, RoutedEventArgs e)
        {
            var selectedDate = dtpFilterDate.SelectedDate;
            _filterModel = new FilterModel()
            {
                CreatedDate = selectedDate != null ? selectedDate.Value : null,
                MemberName = cbFilterMembers.SelectedItem.ToString()
            };
            LoadReportByWeek(_filterModel);
        }
    }
}