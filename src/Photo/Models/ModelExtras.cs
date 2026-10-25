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


    // Так позиция выглядит в составе заказа.
    public override string ToString() => $"{Hall?.Name ?? "Зал не указан"} · {Photographer?.User?.FullName ?? "Фотограф не назначен"} · {Hall?.EquipmentDescription ?? "Оборудование не указано"} · {TotalCost:N2} руб.";


}


public partial class Status
{
    public override string ToString() => Name;
}



public partial class Photographer
{
    // Как фотограф выглядит в списке: ФИО пользователя · Цена за час
    // Берем имя через навигацию: Photographer -> User -> FullName
    public override string ToString() => $"{User?.FullName ?? "Неизвестный"} · {Price} руб./час";

 


}





//public partial class OrderItem
//{
//    // Стоимость строки заказа: количество × цена одного места.
//    public decimal Total => Quantity * UnitPrice;


//    // Так позиция выглядит в составе заказа.
//    public override string ToString() =>
//        $"{Variant?.Course?.CourseName} / {Variant?.VariantName} / " + $"{Quantity} × {UnitPrice:N2} = {Total:N2} руб.";
//}










