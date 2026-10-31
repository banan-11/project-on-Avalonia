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
        // ВАЖНО: поставь реальный ID статуса "Отменён" из своей БД
        private const int CancelledStatusId = 3;

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
                var date = DateOnly.FromDateTime(DateBox.SelectedDate.Value);
                var startTime = new TimeOnly((int)StartHourBox.Value!, (int)StartMinuteBox.Value!);
                var hours = DurationBox.Value ?? 1;

                var newStart = date.ToDateTime(startTime);
                var newEnd = newStart.AddHours((double)hours);

                if (newStart < DateTime.Now)
                {
                    MainWindow.Instance.ShowMessage("Нельзя забронировать время в прошлом.");
                    return;
                }

                using var db = Db.Create();

                // Берем заказы за соседние дни тоже, чтобы поймать пересечение через полночь
                var from = date.AddDays(-1);
                var to = date.AddDays(1);

                var candidates = await db.Orders
                    .AsNoTracking()
                    .Where(o => o.StatusId != CancelledStatusId
                             && o.BookingDate >= from && o.BookingDate <= to
                             && (o.HallId == HallId || o.PhotographerId == photographer.PhotographerId))
                    .ToListAsync();

                // Само сравнение делаем в памяти, EF плохо переводит арифметику над TimeOnly
                var conflict = candidates.FirstOrDefault(o =>
                {
                    var s = o.BookingDate.ToDateTime(o.StartTime);
                    var end = s.AddHours(Convert.ToDouble(o.HoursCount));
                    return newStart < end && s < newEnd;
                });

                if (conflict != null)
                {
                    var what = conflict.HallId == HallId ? "Зал" : "Фотограф";
                    var cs = conflict.BookingDate.ToDateTime(conflict.StartTime);
                    var ce = cs.AddHours(Convert.ToDouble(conflict.HoursCount));

                    MainWindow.Instance.ShowMessage(
                        $"{what} уже занят с {cs:HH:mm} до {ce:HH:mm} ({cs:dd.MM.yyyy}). Выберите другое время.");
                    return;
                }

                var order = new Order
                {
                    HallId = HallId,
                    PhotographerId = photographer.PhotographerId,
                    UserId = Session.CurrentUser.UserId,
                    BookingDate = date,
                    StartTime = startTime,
                    HoursCount = hours,
                    StatusId = 1,
                    CreatedAt = DateTime.Now
                };

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