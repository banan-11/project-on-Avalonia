using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
using Photo.Data;
using Photo.Models;
using Photo.Services;
using static System.Collections.Specialized.BitVector32;


namespace Photo.Views
{
    public partial class CourseView : UserControl
    {
        // Номер открытой услуги. Задаётся при создании страницы:
        //     new CourseView { CourseId = 5 }
        public int CourseId { get; set; }

        // Цена, которую увидел пользователь. При сохранении сравним её с текущей.
        private decimal shownPrice;


        public CourseView()
        {
            InitializeComponent();

            Loaded += async (s, e) => await LoadCourseAsync();



        }

        // Загрузить услугу, её потоки и (для сотрудника) список клиентов.
        private async Task LoadCourseAsync()
        {
            try
            {
                SaveButton.IsEnabled = false; // пока данные не загружены — не оформляем
                using var db = Db.Create();
                Course? course = await db.Courses
                    .Include(c => c.CourseVariants)   // вместе с потоками
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.CourseId == CourseId);
                if (course == null)
                    throw new AppException("Услуга не найдена. Вернитесь в каталог.");
                // Заполняем элементы страницы.
                NameText.Text = course.CourseName;
                DescriptionText.Text = course.Description;
                CourseImage.Source = UiSupport.Picture(course.ImagePath);
                shownPrice = await CatalogService.GetPriceAsync(db, course);
                PriceText.Text = $"Цена одного места: {shownPrice:N2} руб.";
                // Только потоки, где есть свободные места.
                List<CourseVariant> variants = course.CourseVariants
                    .Where(v => v.AvailableSeats > 0)
                    .OrderBy(v => v.VariantName)
                    .ToList();
                VariantBox.ItemsSource = variants;
                VariantBox.SelectedIndex = variants.Count > 0 ? 0 : -1; // -1 = ничего не выбрано
                                                                        // Выбор клиента — только для методиста и администратора.
                ClientPanel.IsVisible = Session.IsStaff;
                if (Session.IsStaff)
                {
                    List<User> users = await Session.GetUsersAsync();
                    ClientBox.ItemsSource = users;
                    // По умолчанию выбран сам сотрудник.
                    ClientBox.SelectedItem = users.FirstOrDefault(
                        u => u.UserId == Session.CurrentUser!.UserId);
                }
                SaveButton.IsEnabled = variants.Count > 0;
                if (variants.Count == 0)
                    MainWindow.Instance.ShowMessage("Свободных мест нет ни в одном потоке.");
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }



        // Кнопка «Оформить».
        private async void SaveClick(object? sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false; // защита от двойного щелчка
            try
            {
                // 1) Поток.
                CourseVariant? variant = VariantBox.SelectedItem as CourseVariant;
                if (variant == null)
                    throw new AppException("Выберите поток.");
                // 2) Количество: TryParse не «роняет» программу, если ввели буквы.
                if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
                    throw new AppException("Количество должно быть целым числом больше нуля.");
                // 3) Клиент: обычный пользователь — сам, сотрудник — выбранный в списке.
                int clientId = Session.CurrentUser!.UserId;
                if (Session.IsStaff)
                {
                    User? client = ClientBox.SelectedItem as User;
                    if (client == null)
                        throw new AppException("Выберите клиента.");
                    clientId = client.UserId;
                }
                // 4) Сохранение. Все проверки мест и цены — внутри OrderService.
                int orderId = await OrderService.CreateOrderAsync(
                    variant.VariantId, quantity, clientId, shownPrice);
                // 5) Успех: открываем свежий каталог и показываем номер заказа.
                MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
                MainWindow.Instance.ShowMessage($"Заказ № {orderId} оформлен. Каталог обновлён.");
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
                SaveButton.IsEnabled = true; // ошибка — можно исправить и нажать снова
            }
        }










    }
}