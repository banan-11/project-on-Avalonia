using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore; // Для работы с БД
using Photo.Data;      // Db.Create()
using Photo.Models;    // Hall, Photographer, Order
using Photo.Services;  // UiSupport, Session
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Photo.Views
{
    public partial class CourseView : UserControl
    {
        // ID зала, переданный из каталога
        public int HallId { get; set; }

        private List<Photographer> _allPhotographers = new();

        public CourseView()
        {
            InitializeComponent();

            // Подписываемся на изменения значений для пересчета цены
            PhotographerBox.SelectionChanged += OnSelectionChanged;
            DurationBox.ValueChanged += OnNumericValueChanged;
            StartHourBox.ValueChanged += OnNumericValueChanged;
            StartMinuteBox.ValueChanged += OnNumericValueChanged;

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var db = Db.Create();

                // 1. ЗАГРУЖАЕМ ИНФО О ЗАЛЕ ИЗ БАЗЫ ДАННЫХ
                var hall = await db.Halls
                    .AsNoTracking()
                    .SingleOrDefaultAsync(h => h.HallId == HallId);

                if (hall == null)
                {
                    MainWindow.Instance.ShowError(new Exception("Зал не найден в базе данных"));
                    return;
                }

                // Заполняем левую часть данными из колонок SQL
                HallNameText.Text = hall.Name;

                // Берем описание оборудования. Если NULL или пусто - пишем заглушку.
                EquipmentText.Text = string.IsNullOrEmpty(hall.EquipmentDescription)
                    ? "Нет описания оборудования"
                    : hall.EquipmentDescription;

                HallImage.Source = UiSupport.Picture(hall.ImagePath);

                // 2. ЗАГРУЖАЕМ СПИСОК ФОТОГРАФОВ
                // Include(p => p.User) нужен, чтобы в ToString() работал FullName пользователя
                _allPhotographers = await db.Photographers
                    .Include(p => p.User)
                    .AsNoTracking()
                    .ToListAsync();

                PhotographerBox.ItemsSource = _allPhotographers;

                if (_allPhotographers.Count > 0)
                    PhotographerBox.SelectedIndex = 0;

                UpdatePriceDisplay();
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        // Пересчет итоговой суммы
        private void UpdatePriceDisplay()
        {
            if (PhotographerBox.SelectedItem is not Photographer selectedPhotographer)
            {
                PriceHintText.Text = "Выберите фотографа";
                TotalCostText.Text = "-- руб.";
                return;
            }

            decimal hourlyRate = selectedPhotographer.Price;
            int hours = (int)(DurationBox.Value ?? 1);
            decimal total = hourlyRate * hours;

            PriceHintText.Text = $"Ставка мастера: {hourlyRate:N2} руб./час";
            TotalCostText.Text = $"{total:N2} руб.";
        }

        // Обработчик смены фотографа
        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdatePriceDisplay();
        }

        // Обработчик смены времени/длительности
        private void OnNumericValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            UpdatePriceDisplay();
        }

        // Кнопка "Забронировать"
        private async void BookClick(object? sender, RoutedEventArgs e)
        {
            if (Session.CurrentUser == null)
            {
                MainWindow.Instance.ShowMessage("Сначала войдите в систему.");
                return;
            }

            if (PhotographerBox.SelectedItem is not Photographer photographer)
            {
                MainWindow.Instance.ShowMessage("Выберите фотографа.");
                return;
            }

            if (DateBox.SelectedDate == null)
            {
                MainWindow.Instance.ShowMessage("Выберите дату съемки.");
                return;
            }

            try
            {
                // Создаем объект заказа
                var order = new Order
                {
                    HallId = HallId,
                    PhotographerId = photographer.PhotographerId,
                    UserId = Session.CurrentUser.UserId,
                    BookingDate = DateOnly.FromDateTime(DateBox.SelectedDate.Value),
                    StartTime = new TimeOnly((int)StartHourBox.Value!, (int)StartMinuteBox.Value!),
                    HoursCount = DurationBox.Value ?? 1,
                    StatusId = 1, // Статус "Подтверждён" (проверь ID статуса в своей БД!)
                    CreatedAt = DateTime.Now
                };

                using var db = Db.Create();
                db.Orders.Add(order);
                await db.SaveChangesAsync();

                MainWindow.Instance.ShowMessage($"Бронь № {order.OrderId} успешно создана!");
                MainWindow.Instance.OpenPage(new OrdersView());
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        // Кнопка "Отмена"
        private void CancelClick(object? sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenPage(new CatalogView());
        }
    }
}