using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media; // Для SolidColorBrush и Color
using Microsoft.EntityFrameworkCore;
using Photo.Data;
using Photo.Models;
using Photo.Services;

namespace Photo.Views
{
    public partial class OrderDetailsView : UserControl
    {
        public int OrderId { get; set; }

        public OrderDetailsView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadOrderAsync();
        }

        private async Task LoadOrderAsync()
        {
            try
            {
                ConfirmPanel.IsVisible = false;

                using var db = Db.Create();

                var order = await db.Orders
                    .Include(o => o.Hall)
                    .Include(o => o.User)
                    .Include(o => o.Photographer)
                        .ThenInclude(p => p.User)
                    .Include(o => o.Status)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(o => o.OrderId == OrderId);

                if (order == null)
                {
                    MainWindow.Instance.ShowMessage("Заказ не найден.");
                    GoBackClick(null, null);
                    return;
                }

                // Заполняем данные
                HeaderText.Text = $"Бронирование № {order.OrderId}";
                ClientText.Text = order.User?.FullName ?? "Неизвестный клиент";
                HallText.Text = order.Hall?.Name ?? "Зал удален";
                PhotographerText.Text = order.Photographer?.ToString() ?? "Без фотографа";
                DateText.Text = order.BookingDate.ToString("dd.MM.yyyy");
                TimeText.Text = $"{order.StartTime:hh\\:mm} ({order.HoursCount} ч.)";
                TotalText.Text = $"{order.TotalCost:N2} руб.";

                // Логика цвета статуса
                StatusText.Text = order.Status?.Name ?? "Без статуса";

                // Меняем цвет бейджика статуса в зависимости от текста
                var statusBorder = StatusText.Parent as Border;
                if (statusBorder != null)
                {
                    if (order.Status?.Name == "Подтверждён")
                    {
                        statusBorder.Background = new SolidColorBrush(Color.Parse("#ECFDF5")); // Светло-зеленый
                        StatusText.Foreground = new SolidColorBrush(Color.Parse("#059669")); // Темно-зеленый
                    }
                    else if (order.Status?.Name == "Отменён")
                    {
                        statusBorder.Background = new SolidColorBrush(Color.Parse("#FEF2F2")); // Светло-красный
                        StatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626")); // Темно-красный
                    }
                    else
                    {
                        statusBorder.Background = new SolidColorBrush(Color.Parse("#F3F4F6")); // Серый
                        StatusText.Foreground = new SolidColorBrush(Color.Parse("#4B5563")); // Темно-серый
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        private void CancelOrderClick(object? sender, RoutedEventArgs e)
        {
            ConfirmPanel.IsVisible = true;
        }

        private void CancelDeleteClick(object? sender, RoutedEventArgs e)
        {
            ConfirmPanel.IsVisible = false;
        }

        private async void ConfirmDeleteClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                using var db = Db.Create();
                var orderToDelete = await db.Orders.FindAsync(OrderId);

                if (orderToDelete != null)
                {
                    // Вместо удаления можно менять статус на "Отменён", если нужно сохранять историю
                    // orderToDelete.StatusId = 3; 
                    // await db.SaveChangesAsync();

                    // Или удаляем физически, как было раньше:
                    db.Orders.Remove(orderToDelete);
                    await db.SaveChangesAsync();

                    MainWindow.Instance.ShowMessage($"Бронирование № {OrderId} успешно отменено.");
                    GoBackClick(null, null);
                }
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        private void GoBackClick(object? sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenPage(new OrdersView());
        }
    }
}