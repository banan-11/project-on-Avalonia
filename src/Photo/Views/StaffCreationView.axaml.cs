using Avalonia.Controls;
using Avalonia.Interactivity;
using Photo.Services; // Для Session и AppException

namespace Photo.Views
{
    public partial class StaffCreationView : UserControl
    {
        public StaffCreationView()
        {
            InitializeComponent();
        }

        // Меняем видимость поля цены при выборе роли
        private void RoleChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (RoleSelect.SelectedItem is ComboBoxItem item)
            {
                string roleTag = item.Tag?.ToString() ?? "";
                // Показываем поле цены только если выбрана роль "Фотограф"
                PricePanel.IsVisible = roleTag == "Фотограф";
            }
        }

        private async void SaveClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Получаем выбранную роль
                if (!(RoleSelect.SelectedItem is ComboBoxItem selectedItem))
                    throw new AppException("Выберите роль сотрудника.");

                string roleName = selectedItem.Tag?.ToString() ?? "";
                if (string.IsNullOrEmpty(roleName))
                    throw new AppException("Некорректная роль.");

                // 2. Получаем данные из полей
                string login = (StaffLogin.Text ?? "").Trim();
                string pass = (StaffPass.Text ?? "").Trim();
                string first = (StaffFirst.Text ?? "").Trim();
                string last = (StaffLast.Text ?? "").Trim();
                string phoneStr = (StaffPhone.Text ?? "").Trim();
                string email = (StaffEmail.Text ?? "").Trim();

                // === НОВАЯ ВАЛИДАЦИЯ ИМЕНИ И ФАМИЛИИ ===
                if (string.IsNullOrWhiteSpace(first))
                    throw new AppException("⛔ Имя сотрудника обязательно для заполнения.");

                if (string.IsNullOrWhiteSpace(last))
                    throw new AppException("⛔ Фамилия сотрудника обязательна для заполнения.");

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
                    throw new AppException("Логин и пароль обязательны.");

                if (!int.TryParse(phoneStr, out int phone))
                    throw new AppException("Телефон должен быть числом.");

                // 3. Готовим цену для фотографа (если нужно)
                decimal? price = null;
                if (roleName == "Фотограф")
                {
                    price = StaffPrice.Value ?? 0;
                }

                // 4. Вызываем сервис создания через Session
                int? newUserId = await Session.CreateStaffAsync(
                    login, pass, first, last, phone, email, roleName, price);

                if (newUserId == null)
                    throw new AppException("Ошибка сохранения. Возможно, такой логин уже существует.");

                MainWindow.Instance.ShowMessage($"✅ Сотрудник {first} {last} ({roleName}) успешно добавлен!");

                // Возвращаемся в каталог после успешного создания
                MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        private void CancelClick(object? sender, RoutedEventArgs e)
        {
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
        }
    }
}