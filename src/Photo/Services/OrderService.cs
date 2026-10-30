using System;
using System.Collections.Generic;
using System.Linq; // Нужно для .ToList() и .Any() в памяти
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Photo.Data;
using Photo.Models;
using Photo.Services;

namespace Photo.Services
{
    public static class OrderService
    {
        /// <summary>
        /// Оформить бронирование зала. Возвращает номер нового заказа.
        /// </summary>
        public static async Task<int> CreateBookingAsync(
            int hallId,
            int photographerId,
            DateOnly bookingDate,
            TimeOnly startTime,
            decimal hoursCount,
            int userId)
        {
            // 1. Проверка авторизации
            if (Session.CurrentUser == null)
                throw new AppException("Гость не может оформить заказ. Войдите в систему.");

            if (Session.IsPhotographer)
                throw new AppException("Вы вошли как фотограф. Вы не можете оформлять заказы.");

            using var db = Db.Create();

            // === БЛОК ПРОВЕРКИ КОНФЛИКТОВ (Исправленный) ===

            // Мы загружаем все активные заказы этого зала на эту дату в память.
            // Это надежнее, чем пытаться сделать сложную математику времени внутри SQL запроса.
            var existingOrders = await db.Orders
                .Where(o => o.HallId == hallId &&
                            o.BookingDate == bookingDate &&
                            o.StatusId != 3) // Статус 3 = Отменен
                .Select(o => new { o.StartTime, o.HoursCount })
                .ToListAsync();

            // Вычисляем время окончания новой брони
            TimeOnly newEndTime = startTime.AddHours((double)hoursCount);

            foreach (var existing in existingOrders)
            {
                // Вычисляем время окончания существующей брони
                TimeOnly existingEndTime = existing.StartTime.AddHours((double)existing.HoursCount);

                // Логика пересечения интервалов:
                // Интервалы пересекаются, если НачалоА < КонецБ И НачалоБ < КонецА
                bool isOverlapping = (startTime < existingEndTime) && (existing.StartTime < newEndTime);

                if (isOverlapping)
                {
                    // Если интервалы пересекаются (включая случай полного совпадения 10:00 == 10:00)
                    throw new AppException($"⛔ Конфликт времени! Зал уже забронирован с {existing.StartTime:hh\\:mm} до {existingEndTime:hh\\:mm}.");
                }
            }

            // === ЕСЛИ КОНФЛИКТОВ НЕТ -> СОХРАНЯЕМ ===

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var hall = await db.Halls.FindAsync(hallId);
                if (hall == null) throw new AppException("Зал не найден.");

                var photographer = await db.Photographers.FindAsync(photographerId);
                if (photographer == null) throw new AppException("Фотограф не найден.");

                var order = new Order
                {
                    HallId = hallId,
                    PhotographerId = photographerId,
                    UserId = userId,
                    BookingDate = bookingDate,
                    StartTime = startTime,
                    HoursCount = hoursCount,
                    StatusId = 1,
                    CreatedAt = DateTime.Now
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();
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