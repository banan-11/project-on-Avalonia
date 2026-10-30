using Avalonia.Controls;
using Avalonia.Interactivity;
using Photo.Services; // Для Session и AppException

namespace Photo.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    // ---------- ЛОГИКА ВХОДА ----------
    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "";

        try
        {
            string login = (LoginBox.Text ?? "").Trim();
            string pass = PasswordBox.Text ?? "";

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
                throw new AppException("Введите логин и пароль.");

            // Проверяем данные в базе через сервис
            await Session.LoginAsync(login, pass);

            // Успешный вход: переходим в Каталог.
            // Если это Админ, он увидит дополнительные кнопки благодаря проверке IsAdmin в MainWindow/CatalogView.
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
        }
        catch (AppException ex)
        {
            StatusText.Foreground = Avalonia.Media.Brushes.DarkRed;
            StatusText.Text = ex.Message;
        }
        catch (Exception ex)
        {
            StatusText.Foreground = Avalonia.Media.Brushes.DarkRed;
            StatusText.Text = "Ошибка системы: " + ex.Message;
        }
    }

    // ---------- ПЕРЕХОД НА РЕГИСТРАЦИЮ КЛИЕНТА ----------
    private void GoToRegisterClick(object? sender, RoutedEventArgs e)
    {
        // Открываем страницу регистрации обычного пользователя
        MainWindow.Instance.OpenPage(new RegistrationView());
    }
}