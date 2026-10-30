using Avalonia.Controls;
using Avalonia.Interactivity;
using Photo.Views;
using Microsoft.EntityFrameworkCore; // Для работы с БД
using Photo.Data;                    // Для доступа к Db.Create()
using Photo.Models;                  // Для типов User, Role
using Photo.Services;                // Для AppException и Session
using System;                        // Для Task.Delay
using System.Threading.Tasks;        // Для async/await

// УДАЛЕНО: using Photo.Views; (вызывало конфликт)

namespace Photo.Views;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
    }

    private async void SubmitClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Собираем данные из полей формы
            string login = (RegLogin.Text ?? "").Trim();
            string pass = (RegPassword.Text ?? "").Trim();
            string first = (RegFirstName.Text ?? "").Trim();
            string last = (RegLastName.Text ?? "").Trim();
            string phoneStr = (RegPhone.Text ?? "").Trim();
            string email = (RegEmail.Text ?? "").Trim();

            // 2. Валидация обязательных полей
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
                throw new AppException("Логин и пароль обязательны.");

            if (!int.TryParse(phoneStr, out int phone))
                throw new AppException("Телефон должен содержать только цифры.");

            // 3. Регистрация нового клиента напрямую через БД
            using var db = Db.Create();

            // Проверяем, занят ли логин
            bool exists = await db.Users.AnyAsync(u => u.Login == login);
            if (exists)
                throw new AppException("Такой логин уже занят.");

            // Находим ID роли "Пользователь" (обычно это 3, но лучше искать по имени)
            var clientRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Пользователь");
            if (clientRole == null)
                throw new AppException("Роль 'Пользователь' не найдена в базе данных.");

            // Создаем объект нового пользователя
            var newUser = new User
            {
                Login = login,
                PasswordHash = pass, // Сохраняем пароль как есть (для учебного проекта)
                Name = first,
                LastName = last,
                Phone = phone,
                Email = email,
                RoleId = clientRole.RoleId
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(); // Записываем в базу

            // 4. Успех: показываем сообщение и возвращаемся на экран входа
            MainWindow.Instance.ShowMessage($"Регистрация успешна! Ваш ID: {newUser.UserId}. Теперь войдите.");

            // Небольшая пауза, чтобы пользователь успел прочитать сообщение
            await Task.Delay(1500);

            MainWindow.Instance.OpenPage(new LoginView(), clearHistory: true);
        }
        catch (AppException ex)
        {
            // Отображаем понятные ошибки валидации
            MainWindow.Instance.ShowError(ex);
        }
        catch (Exception ex)
        {
            // Отображаем системные ошибки (проблемы с БД и т.д.)
            MainWindow.Instance.ShowError(new Exception("Ошибка при сохранении данных: " + ex.Message));
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        // Просто возвращаемся на страницу входа без сохранения
        MainWindow.Instance.OpenPage(new LoginView(), clearHistory: true);
    }
}