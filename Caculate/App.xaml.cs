using Caculate.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Web;
using System.IO;
using System.Windows;

namespace Caculate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<CaculateDbContext>(options =>
            {
                var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaculateApp", "caculateapp.db");
                Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
                options.UseSqlite($"Data Source={databasePath}");
            });
            services.AddSingleton<IMemberService, MemberService>();
            services.AddSingleton<IOrderService, OrderService>();
            services.AddScoped<MainWindow>();
            LogManager.Setup().LoadConfigurationFromAppSettings();
        }
        private void OnStartup(object sender, StartupEventArgs e)
        {
            //ensure create database when application start up
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CaculateDbContext>();
            dbContext.Database.EnsureCreated();
            
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }


}
