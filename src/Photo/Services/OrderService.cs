using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Photo.Data;
using Photo.Models;
using Photo.Services;

namespace Photo.Services
{
    public static class OrderService
    {
        private const int MinutesInDay = 24 * 60;

        /// <summary>
        /// Длительность блокировки в минутах: минимум 1 час.
        /// </summary>
        private static int GetBlockMinutes(decimal hoursCount)
        {
            return (int)Math.Round(Math.Max(hoursCount, 1m) * 60m, MidpointRounding.AwayFromZero);
        }

        private static int ToMinutes(TimeOnly time)
        {
            return time.Hour * 60 + time.Minute;
        }

        private static string FormatMinutes(int minutes)
        {
            // Для сообщения об ошибке: 1440 минут показываем как 00:00
            minutes %= MinutesInDay;
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }

        /// <summary>
        /// Оформить бронирование зала с защитой от дубликатов и гонок данных.
        /// </summary>
        public static async Task<int> CreateBookingAsync(
            int hallId,
            int photographerId,
            DateOnly bookingDate,
            TimeOnly startTime,
            decimal hoursCount,
            int userId)
        {
            // 1. Проверка прав доступа
            if (Session.CurrentUser == null)
                throw new AppException("Гость не может оформить заказ. Войдите в систему.");

            if (Session.IsPhotographer)
                throw new AppException("Фотографы не могут создавать заказы.");

            // Проверка на корректность введенных данных
            if (hoursCount <= 0)
                throw new AppException("Количество часов должно быть больше нуля.");

            // Время считаем в минутах от начала суток, а не через TimeOnly.AddHours:
            // TimeOnly циклический, и 23:00 + 2 ч даст 01:00, что ломает проверку пересечений.
            int newStart = ToMinutes(startTime);
            int newEnd = newStart + GetBlockMinutes(hoursCount);

            if (newEnd > MinutesInDay)
                throw new AppException("Бронирование не может заканчиваться после полуночи. Выберите более раннее время или меньшее количество часов.");

            using var db = Db.Create();

            // === НАЧАЛО ТРАНЗАКЦИИ ===
            // Уровень изоляции Serializable защищает от одновременного бронирования
            using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                // 2. Проверка существования зала и фотографа
                var hall = await db.Halls.FindAsync(hallId);
                if (hall == null) throw new AppException("Зал не найден.");

                var photographer = await db.Photographers.FindAsync(photographerId);
                if (photographer == null) throw new AppException("Фотограф не найден.");

                // 3. ПРОВЕРКА КОНФЛИКТОВ ВРЕМЕНИ
                var existingOrders = await db.Orders
                    .Where(o => o.HallId == hallId &&
                                o.BookingDate == bookingDate &&
                                o.StatusId != 3) // 3 = Отменен
                    .Select(o => new { o.StartTime, o.HoursCount })
                    .ToListAsync();

                foreach (var existing in existingOrders)
                {
                    // Каждая бронь блокирует зал минимум на 1 час
                    int exStart = ToMinutes(existing.StartTime);
                    int exEnd = exStart + GetBlockMinutes(existing.HoursCount);

                    // Пересечение интервалов [start, end)
                    bool isOverlapping = newStart < exEnd && exStart < newEnd;

                    if (isOverlapping)
                    {
                        throw new AppException(
                            $"⛔ Конфликт времени! Зал занят с {FormatMinutes(exStart)} до {FormatMinutes(exEnd)}. Пожалуйста, выберите другое время.");
                    }
                }

                // 4. Создание заказа
                var order = new Order
                {
                    HallId = hallId,
                    PhotographerId = photographerId,
                    UserId = userId,
                    BookingDate = bookingDate,
                    StartTime = startTime,
                    HoursCount = hoursCount, // Записываем реальное время, которое выбрал пользователь
                    StatusId = 1, // 1 - Подтвержден/Новый
                    CreatedAt = DateTime.Now
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                // 5. Подтверждение транзакции
                await transaction.CommitAsync();

                return order.OrderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Получить список заказов в зависимости от роли пользователя.
        /// </summary>
        public static async Task<List<Order>> GetOrdersAsync()
        {
            using var db = Db.Create();

            IQueryable<Order> query = db.Orders
                .Include(o => o.Hall)
                .Include(o => o.User)
                .Include(o => o.Photographer)
                    .ThenInclude(p => p.User)
                .Include(o => o.Status);

            if (Session.IsAdmin)
            {
                // Админ видит ВСЕ заказы.
            }
            else if (Session.IsPhotographer)
            {
                if (Session.CurrentUser == null) return new List<Order>();

                var myProfile = await db.Photographers
                    .FirstOrDefaultAsync(p => p.UserId == Session.CurrentUser.UserId);

                if (myProfile == null) return new List<Order>();

                query = query.Where(o => o.PhotographerId == myProfile.PhotographerId);
            }
            else
            {
                if (Session.CurrentUser == null) return new List<Order>();
                query = query.Where(o => o.UserId == Session.CurrentUser.UserId);
            }

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Получить один конкретный заказ со всеми деталями.
        /// </summary>
        public static async Task<Order?> GetOrderAsync(int orderId)
        {
            using var db = Db.Create();

            var order = await db.Orders
                .Include(o => o.Hall)
                .Include(o => o.User)
                .Include(o => o.Photographer)
                    .ThenInclude(p => p.User)
                .Include(o => o.Status)
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return null;

            if (Session.IsAdmin) return order;

            if (!Session.IsStaff && order.UserId != Session.CurrentUser?.UserId)
                throw new AppException("Нет доступа к этому заказу.");

            if (Session.IsPhotographer)
            {
                var myProfile = await db.Photographers.FirstOrDefaultAsync(p => p.UserId == Session.CurrentUser!.UserId);
                if (myProfile == null || order.PhotographerId != myProfile.PhotographerId)
                    throw new AppException("Нет доступа к этому заказу.");
            }

            return order;
        }

        /// <summary>
        /// Отменить (удалить) бронирование.
        /// </summary>
        public static async Task CancelBookingAsync(int orderId)
        {
            using var db = Db.Create();

            var order = await db.Orders.FindAsync(orderId);
            if (order == null)
                throw new AppException("Заказ не найден.");

            if (!Session.IsStaff && order.UserId != Session.CurrentUser?.UserId)
                throw new AppException("Нельзя отменить чужой заказ.");

            db.Orders.Remove(order);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Получить список занятых дат для конкретного зала (для календаря).
        /// </summary>
        public static async Task<List<DateOnly>> GetBookedDatesAsync(int hallId)
        {
            using var db = Db.Create();

            return await db.Orders
                .Where(o => o.HallId == hallId && o.StatusId != 3)
                .Select(o => o.BookingDate)
                .Distinct()
                .ToListAsync();
        }
    }
}