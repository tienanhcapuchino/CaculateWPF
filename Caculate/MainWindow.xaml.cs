using Caculate.DataContext;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
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
        public MainWindow(IMemberService memberService)
        {
            InitializeComponent();
            dtgOrders.ItemsSource = dataGridOrderModels;
            dtgReport.ItemsSource = dataGridReports;
            dtgOutstanding.ItemsSource = dataGridOutstandings;
            _memberService = memberService;
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
                MessageBox.Show("Please add order detail first");
                return;
            }
            var countIsPayer = dataGridOrderModels.Count(x => x.IsPayer);

            if (countIsPayer > 1)
            {
                MessageBox.Show("You only can select one payer!");
                return;
            }

            if (countIsPayer == 0)
            {
                MessageBox.Show("Please select payer");
                return;
            }

            try
            {
                var totalMoney = dataGridOrderModels.Sum(x => x.Money);
                var payerName = dataGridOrderModels.FirstOrDefault(x => x.IsPayer)?.Name?.Trim();
                if (string.IsNullOrEmpty(payerName))
                {
                    MessageBox.Show("Don't have payer!");
                    return;
                }
                using (var context = new CaculateDbContext())
                {
                    var payer = context.Members.Where(x => x.Name == payerName).Select(x => x.Id).FirstOrDefault();
                    if (payer == Guid.Empty)
                    {
                        MessageBox.Show("Payer is not exist");
                        return;
                    }

                    var order = new Order()
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now.Ticks,
                        TotalMoney = totalMoney,
                        PayerId = payer
                    };
                    context.Orders.Add(order);
                    context.SaveChanges();

                    List<OrderParticipant> orderParticipants = new List<OrderParticipant>();
                    foreach (var item in dataGridOrderModels)
                    {
                        var memberId = context.Members.Where(x => x.Name == item.Name.Trim()).Select(x => x.Id).FirstOrDefault();
                        if (memberId == Guid.Empty)
                        {
                            MessageBox.Show($"Member {item.Name} is not exist");
                            return;
                        }
                        var orderParticipant = new OrderParticipant()
                        {
                            Id = Guid.NewGuid(),
                            MemberId = memberId,
                            Money = item.Money,
                            CreatedDate = item.CreatedDate.Ticks,
                            OrderId = order.Id
                        };
                        orderParticipants.Add(orderParticipant);
                    }
                    if (orderParticipants.Count > 0)
                    {
                        context.OrderParticipants.AddRange(orderParticipants);
                        context.SaveChanges();
                    }
                    MessageBox.Show("Submit order successfully");
                    dataGridOrderModels.Clear();
                    btRefresh_Click(sender, e);
                }
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
                MessageBox.Show("Please enter member name");
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
                    MessageBox.Show("Member name is exist");
                    return;
                }

                var resultAdd = _memberService.AddNewMember(member).GetAwaiter().GetResult();
                if (!resultAdd)
                {
                    MessageBox.Show("Add failed");
                }
                AddedMemberDialog.IsOpen = false;
                MessageBox.Show("Add member successfully");
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
                                MessageBox.Show($"Member: {member.Name} is already existed!");
                                return;
                            }
                            MessageBox.Show($"Updated member: {member.Name}");
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
                    MessageBox.Show("Deleted member!");
                }
                else
                {
                    MessageBox.Show("Delete failed!");
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
                    MessageBox.Show("Please select date first");
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