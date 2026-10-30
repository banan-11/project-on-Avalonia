using Avalonia.Controls;
using Avalonia.Interactivity;
using Photo.Models;
using Photo.Services;
using System.Threading.Tasks;

namespace Photo.Views
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                var orders = await OrderService.GetOrdersAsync();
                OrdersList.ItemsSource = orders;

                if (Session.IsAdmin)
                {
                    TitleText.Text = "Все заказы (Администратор)";
                    NewButton.IsVisible = true;
                }
                else if (Session.IsPhotographer)
                {
                    TitleText.Text = "Мой рабочий график";
                    NewButton.IsVisible = false;
                }
                else if (Session.CurrentUser != null)
                {
                    TitleText.Text = "Мои заказы";
                    NewButton.IsVisible = true;
                }
                else
                {
                    TitleText.Text = "Гостевой режим";
                    NewButton.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadOrdersAsync();

        private void NewClick(object? sender, RoutedEventArgs e)
        {
            if (Session.IsPhotographer)
            {
                MainWindow.Instance.ShowMessage("Фотографы не могут создавать заказы.");
                return;
            }
            MainWindow.Instance.OpenPage(new CatalogView());
        }

        private void OpenClick(object? sender, RoutedEventArgs e)
        {
            if (OrdersList.SelectedItem is Order order)
            {
                MainWindow.Instance.OpenPage(new OrderDetailsView { OrderId = order.OrderId });
            }
            else
            {
                MainWindow.Instance.ShowMessage("Сначала выберите заказ в списке.");
            }
        }
    }
}