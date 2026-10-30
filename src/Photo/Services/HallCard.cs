using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Imaging;


namespace Photo.Services
{
    // Одна карточка каталога: всё, что нужно показать на экране для одной услуги.
    // Разметка CatalogView.axaml берёт значения отсюда через {Binding ИмяСвойства}.
    // Значения заполняет метод CatalogService.LoadCardsAsync.
    public class HallCard
    {

        //public int Id { get; set; }                  // course_id — чтобы открыть услугу
        //public string Name { get; set; } = "";       // название
        //public string Code { get; set; } = "";       // код (для поиска)
        //public string Category { get; set; } = "";   // название категории
        //public string Department { get; set; } = ""; // организатор (подразделение)
        //public string Content { get; set; } = "";    // состав курса
        //public int Seats { get; set; }               // сумма свободных мест всех потоков
        //public string SeatsText { get; set; } = "";  // «Мест: много» / «Мест: мало»
        //public decimal Price { get; set; }           // цена (для сортировки)
        //public string PriceText { get; set; } = "";  // цена для показа: «750,00 руб.»




        //public int HallId { get; set; }

        //public string Name { get; set; } = null!;

        //public string? ImagePath { get; set; }

        //public string? EquipmentDescription { get; set; }


        //public IBrush? Background { get; set; }      // фон карточки (красный, если мало мест)
        //public Bitmap? Image { get; set; }           // картинка или заглушка






        
        
            public int Id { get; set; }           // ID зала (для перехода)
            public string Name { get; set; } = ""; // Название зала

            // Описание оборудования (аналог Content/Seats из методички)
            public string Description { get; set; } = "";

            // Цена за час (рассчитывается динамически со скидкой)
            public decimal Price { get; set; }
            public string PriceText { get; set; } = ""; // Форматированная строка цены

            // Статус активности ("Популярный" / "Свободный") вместо "Много/Мало мест"
            public string ActivityLabel { get; set; } = "";

            // Цвет фона (красный, если низкая активность)
            public IBrush? Background { get; set; }

            // Картинка или заглушка
            public Bitmap? Image { get; set; }
        

















    }
}
