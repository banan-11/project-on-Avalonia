using System;
using System.Collections.Generic;
using System.Text;

namespace Photo.Services
{
    // Правила расчёта. Здесь нет ни базы данных, ни окон — только арифметика.
    // Поэтому эти методы легко проверить отдельной программой (tests/RulesChecks).
    public static class PriceRules

    {




        // Границы предыдущего календарного месяца.
        // Используется для проверки истории заказов (была ли активность?).
        public static (DateOnly Start, DateOnly End) PreviousMonth(DateOnly day)
        {
            DateOnly end = new DateOnly(day.Year, day.Month, 1); // 1-е число текущего месяца
            DateOnly start = end.AddMonths(-1);                  // 1-е число прошлого месяца
            return (start, end);                                 // Возвращаем пару дат
        }


        // Расчёт итоговой цены за один час аренды с учётом скидки.
        // <param name="hadOrdersLastMonth">True, если в прошлом месяце были заказы этого зала.</param>
        // <returns>Цена за час после округления до копеек.</returns>
        public static decimal Price(decimal basePrice, bool hadOrdersLastMonth)
        {
            // ПРАВИЛО СКИДКИ:
            // Если заказов НЕ было -> скидка 25% (множитель 0.75).
            // Если заказы БЫЛИ -> полная стоимость (множитель 1.0).
            // Можно поменять 0.75m на другую цифру, если преподаватель требует иную скидку.
            decimal multiplier = hadOrdersLastMonth ? 1.0m : 0.75m;

            decimal finalPrice = basePrice * multiplier;

            // Округляем до 2 знаков после запятой (копейки), стандартное банковское округление
            return decimal.Round(finalPrice, 2, MidpointRounding.AwayFromZero);
        }


        // Текст статуса популярности зала (аналог "много/мало" из методички).
        // Для фотостудии можно использовать количество бронирований за месяц. 
        public static string PopularityLabel(int bookingsCount)
        {
            // Если забронировано больше 5 раз - "популярный", иначе - "свободный"
            // Эти пороги (5) можно менять по вкусу.
            return bookingsCount > 5 ? "Популярный" : "Свободный";
        }


  
        // Пример: если заказов меньше 3, считаем зал "низкой активности".
        public static bool IsLowActivity(int bookingsCount)
        {
            return bookingsCount <= 3;
        }






















    }
}
