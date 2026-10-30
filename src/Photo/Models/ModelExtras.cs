namespace Photo.Models;

public partial class User
{
    // ФИО одной строкой. Trim убирает пробел в конце, если отчества нет.
    public string FullName => $"{LastName} {Name}".Trim();

    // ToString решает, как объект выглядит в ComboBox и ListBox.
    public override string ToString() => $"{Login} — {FullName}";
}

public partial class Hall
{
    // Вывод Имя и оборудования зала 
    public override string ToString() => $"{Name} · оборудование зала: {EquipmentDescription}";
}

public partial class Order
{
    // Свойство для расчета итоговой стоимости: Ставка фотографа * Часы аренды.
    public decimal TotalCost => (Photographer?.Price ?? 0) * HoursCount;

    // Так позиция выглядит в составе заказа и в списке.
    public override string ToString()
    {
        // 1. Формируем строку про фотографа отдельно, чтобы обработать все случаи
        string photographerText = "Фотограф не назначен";

        if (Photographer != null)
        {
            // Если есть объект фотографа, пробуем взять имя пользователя
            if (Photographer.User != null)
            {
                photographerText = $"{Photographer.User.FullName} ({Photographer.Price} руб./час)";
            }
            else
            {
                // Если User не подгрузился (редкий случай при Include), но ID есть
                photographerText = $"Фотограф ID:{Photographer.PhotographerId} ({Photographer.Price} руб./час)";
            }
        }

        // 2. Собираем итоговую строку
        // Формат: Зал · Фотограф · Описание · Цена
        return $"{Hall?.Name ?? "Зал удален"} · {photographerText} · {Hall?.EquipmentDescription ?? ""} · {TotalCost:N2} руб.";
    }
}

public partial class Status
{
    public override string ToString() => Name;
}

public partial class Photographer
{
    // Как фотограф выглядит в списке: ФИО пользователя · Цена за час
    public override string ToString() => $"{User?.FullName ?? "Неизвестный"} · {Price} руб./час";
}