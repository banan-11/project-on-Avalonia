using Microsoft.EntityFrameworkCore;
using Photo.Data;      // Для доступа к Db.Create()
using Photo.Models;    // Для типов User, Role, Photographer
using Photo.Services;  // Для AppException

namespace Photo.Services;

// Session («сеанс») помнит, кто сейчас работает с программой.
public static class Session
{
    // Текущий пользователь. null означает «гость» (никто не вошёл).
    public static User? CurrentUser { get; private set; }

    // Название роли текущего пользователя; для гостя — пустая строка.
    public static string RoleName => CurrentUser?.Role.RoleName ?? "";

    // Уровни доступа.
    // АДАПТАЦИЯ: названия ролей пишите ТОЧНО как в таблице roles вашей БД.
    public static bool IsGuest => CurrentUser == null;

    // Администратор имеет полный доступ
    public static bool IsAdmin => RoleName == "Администратор";

    // НОВОЕ СВОЙСТВО: Фотограф (исполнитель)
    public static bool IsPhotographer => RoleName == "Фотограф";

    // Staff включает тех, кому нужны расширенные права просмотра (Админы и Фотографы).
    // Клиенты ("Пользователи") не входят в Staff.
    public static bool IsStaff => IsAdmin || IsPhotographer;

    // Подпись в шапке окна.
    public static string Caption =>
        CurrentUser == null ? "Гость" : $"{CurrentUser.FullName} · {RoleName}";

    // Все пользователи для выпадающего списка (вместе с ролями).
    public static async Task<List<User>> GetUsersAsync()
    {
        using var db = Db.Create();
        return await db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Login)
            .AsNoTracking()
            .ToListAsync();
    }

    // Вход по логину и паролю
    public static async Task LoginAsync(string login, string password)
    {
        using var db = Db.Create();

        // Ищем пользователя по логину (без учета регистра)
        var user = await db.Users
            .Include(u => u.Role)       // Обязательно загружаем роль!
            .FirstOrDefaultAsync(u => u.Login.ToLower() == login.Trim().ToLower());

        if (user == null)
            throw new AppException("Неверный логин или пароль.");

        // Проверяем пароль. 
        // Для учебного проекта сравниваем строки напрямую (как у вас в SQL 'admin').
        if (user.PasswordHash != password)
            throw new AppException("Неверный логин или пароль.");

        // Сохраняем текущего пользователя
        CurrentUser = user;
    }

    // Выход: снова гость.
    public static void Logout()
    {
        CurrentUser = null;
    }

    // Проверки прав. Вызываются в сервисах ПЕРЕД изменением данных.
    public static void RequireStaff()
    {
        if (!IsStaff)
            throw new AppException("Недостаточно прав: нужна роль сотрудника или администратора.");
    }

    public static void RequireAdmin()
    {
        if (!IsAdmin)
            throw new AppException("Недостаточно прав: нужна роль администратора.");
    }

    // ---------------------------------------------------------
    // МЕТОД СОЗДАНИЯ СОТРУДНИКА (Для кнопки "Персонал")
    // ---------------------------------------------------------
    public static async Task<int?> CreateStaffAsync(
        string login,
        string password,
        string firstName,
        string lastName,
        int phone,
        string email,
        string roleName,
        decimal? photographerPrice = null)
    {
        RequireAdmin(); // Только админ может создавать персонал

        using var db = Db.Create();

        // 1. Проверяем уникальность логина
        if (await db.Users.AnyAsync(u => u.Login == login))
            return null; // Логин занят

        // 2. Находим ID роли по её названию ("Администратор" или "Фотограф")
        var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null)
            throw new AppException($"Роль '{roleName}' не найдена в базе данных.");

        // 3. Создаем объект User
        var newUser = new User
        {
            Login = login.Trim(),
            PasswordHash = password, // Сохраняем пароль как есть (для учебной БД)
            Name = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = phone,
            Email = email.Trim(),
            RoleId = role.RoleId
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync(); // Сохраняем пользователя, чтобы получить его UserId

        // 4. Если это Фотограф, создаем запись в таблице Photographers
        if (roleName == "Фотограф" && photographerPrice.HasValue)
        {
            var photographerProfile = new Photographer
            {
                UserId = newUser.UserId,
                Price = (int)photographerPrice.Value // Приводим к типу из БД (INT)
            };

            db.Photographers.Add(photographerProfile);
            await db.SaveChangesAsync();
        }

        return newUser.UserId; // Возвращаем ID созданного пользователя
    }
}