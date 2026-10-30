using Microsoft.EntityFrameworkCore;
using Photo.Data;
using Photo.Models;
using Photo.Views;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Photo.Services
{
    public static class CatalogService
    {
        // Текущая цена аренды зала. 
        // ТАК КАК В МОДЕЛИ HALL НЕТ ПОЛЯ ЦЕНЫ, МЫ ИСПОЛЬЗУЕМ ЗАГЛУШКУ ИЛИ ЛОГИКУ ФОТОГРАФА.
        // Для примера возьмем фиксированную базовую цену 500 руб/час.
        private const decimal DefaultBasePrice = 500m;

        public static async Task<decimal> GetPriceAsync(PhotoContext db, Hall hall)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            var (start, end) = PriceRules.PreviousMonth(today);

            // Проверяем историю заказов этого зала
            bool hadOrders = await db.Orders.AnyAsync(o =>
                o.HallId == hall.HallId &&
                o.BookingDate >= start &&
                o.BookingDate < end);

            // Применяем правило скидки к базовой цене
            return PriceRules.Price(DefaultBasePrice, hadOrders);
        }

        public static async Task<List<HallCard>> LoadCardsAsync()
        {
            using var db = Db.Create();

            List<Hall> halls = await db.Halls
                .AsNoTracking()
                .ToListAsync();

            var cards = new List<HallCard>();

            foreach (Hall h in halls)
            {
                var (start, end) = PriceRules.PreviousMonth(DateOnly.FromDateTime(DateTime.Today));

                int bookingsCount = await db.Orders.CountAsync(o =>
                    o.HallId == h.HallId &&
                    o.BookingDate >= start &&
                    o.BookingDate < end);

                decimal price = await GetPriceAsync(db, h);

                cards.Add(new HallCard
                {
                    Id = h.HallId,
                    Name = h.Name,
                    Description = string.IsNullOrEmpty(h.EquipmentDescription)
                        ? "Описание отсутствует"
                        : h.EquipmentDescription,

                    Price = price,
                    PriceText = $"{price:N2} руб./час",

                    ActivityLabel = PriceRules.PopularityLabel(bookingsCount),

                    Background = UiSupport.Brush(PriceRules.IsLowActivity(bookingsCount)
                        ? "LowStockBrush" : "NormalBrush"),

                    Image = UiSupport.Picture(h.ImagePath)
                });
            }
            return cards;
        }
    }
}