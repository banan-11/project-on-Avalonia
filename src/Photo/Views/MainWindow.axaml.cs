using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media; // Для SolidColorBrush и Color
using Photo.Services; // Для Session и UiSupport

namespace Photo.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    // История открытых страниц для кнопки «Назад»
    private readonly Stack<UserControl> history = new();

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        // СТАРТОВАЯ СТРАНИЦА — КАТАЛОГ (пользователь сразу Гость)
        OpenPage(new CatalogView(), clearHistory: true);
    }

    /// <summary>
    /// Открыть страницу. clearHistory: true — начать историю заново (вход, выход).
    /// </summary>
    public void OpenPage(UserControl page, bool clearHistory = false)
    {
        if (clearHistory)
        {
            history.Clear();
        }
        else
        {
            // Текущую страницу кладём в историю, чтобы к ней можно было вернуться.
            UserControl? current = PageHost.Content as UserControl;
            if (current != null)
                history.Push(current);
        }

        ShowPage(page);
    }

    /// <summary>
    /// Показать страницу и обновить шапку и кнопки навигации.
    /// </summary>
    private void ShowPage(UserControl page)
    {
        PageHost.Content = page;                       // Подставить страницу в окно

        // Заголовок окна берется из свойства Tag страницы
        Title = "Фотостудия — " + (page.Tag ?? "");

        MessageText.Text = "";                         // Убрать старое сообщение

        // Обновляем подпись пользователя в шапке ("Гость" или "Иван Иванов · Клиент")
        UserText.Text = Session.Caption;

        // --- ЛОГИКА ВИДИМОСТИ КНОПОК НАВИГАЦИИ ---

        // 1. Кнопка "Заказы"
        // Видна всем авторизованным пользователям: Клиентам, Фотографам и Админам.
        OrdersButton.IsVisible = !Session.IsGuest;

        // 2. Кнопки Администратора
        // Видны ТОЛЬКО администратору.
        bool isAdmin = Session.IsAdmin;

        StaffButton.IsVisible = isAdmin;       // Старая кнопка "Управление"
        EmployeesButton.IsVisible = isAdmin;   // Новая кнопка "Сотрудники"
        HallsButton.IsVisible = isAdmin;       // Новая кнопка "Залы"

        // 3. Кнопка Авторизации/Выхода
        // Меняем текст и стиль в зависимости от состояния сессии.
        if (Session.IsGuest)
        {
            AuthButton.Content = "Войти";
            AuthButton.Background = new SolidColorBrush(Color.Parse("#2563EB")); // Синий
        }
        else
        {
            AuthButton.Content = "Выйти";
            AuthButton.Background = new SolidColorBrush(Color.Parse("#6B7280")); // Серый для выхода
        }

        // 4. Кнопка "Назад"
        BackButton.IsEnabled = history.Count > 0;

        // Скрываем сообщение, если оно пустое (визуальный фикс)
        MessageText.IsVisible = !string.IsNullOrEmpty(MessageText.Text);
    }

    /// <summary>
    /// Обычное информационное сообщение.
    /// </summary>
    public void ShowMessage(string text)
    {
        MessageText.Foreground = new SolidColorBrush(Color.Parse("#059669")); // Зеленый
        MessageText.Text = text;
        MessageText.IsVisible = true;
    }

    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public void ShowError(Exception error)
    {
        System.Diagnostics.Trace.WriteLine(error); // Подробности — в окно Output
        MessageText.Foreground = new SolidColorBrush(Color.Parse("#DC2626")); // Красный
        MessageText.Text = UiSupport.ErrorText(error);
        MessageText.IsVisible = true;
    }

    // ---------- ОБРАБОТЧИКИ СОБЫТИЙ ----------

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        if (history.Count > 0)
            ShowPage(history.Pop()); // Pop — достать последнюю страницу из истории
    }

    private void CatalogClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new CatalogView(), clearHistory: true);
    }

    private void OrdersClick(object? sender, RoutedEventArgs e)
    {
        // Открываем список заказов.
        OpenPage(new OrdersView());
    }

    private void StaffClick(object? sender, RoutedEventArgs e)
    {
        // Открываем форму создания сотрудника (старая кнопка)
        OpenPage(new StaffCreationView());
    }

    // === НОВЫЕ МЕТОДЫ ДЛЯ КНОПОК АДМИНА ===

    private void EmployeesClick(object? sender, RoutedEventArgs e)
    {
        // Открываем страницу управления сотрудниками (редактирование фотографов)
        // Убедись, что файл StaffManagementView создан!
        OpenPage(new StaffManagementView());
    }

    private void HallsClick(object? sender, RoutedEventArgs e)
    {
        // Открываем страницу управления залами (добавление/редактирование залов)
        // Убедись, что файл HallsManagementView создан!И
        OpenPage(new HallsManagementView());
    }

    /// <summary>
    /// Объединенный метод для кнопки авторизации.
    /// </summary>
    private void LogoutOrLoginClick(object? sender, RoutedEventArgs e)
    {
        if (Session.IsGuest)
        {
            // Если гость -> открываем форму ВХОДА
            OpenPage(new LoginView());
        }
        else
        {
            // Если залогинен -> выходим обратно в Гостей (в Каталог)
            Session.Logout();
            OpenPage(new CatalogView(), clearHistory: true);
        }
    }
}