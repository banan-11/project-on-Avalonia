using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using Photo.Data;
using Photo.Models;
using Photo.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Photo.Views;

public partial class StaffManagementView : UserControl
{
    private List<Photographer> _photographers = new();

    public StaffManagementView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadStaffAsync();
    }

    private async Task LoadStaffAsync()
    {
        using var db = Db.Create();
        // Загружаем фотографов и их пользователей
        _photographers = await db.Photographers
            .Include(p => p.User)
            .ToListAsync();

        StaffList.ItemsSource = _photographers;
    }

    private void StaffSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (StaffList.SelectedItem is Photographer p)
        {
            EditName.Text = p.User.Name;
            EditLastName.Text = p.User.LastName;
            EditPhone.Text = p.User.Phone.ToString();
            EditEmail.Text = p.User.Email;
            EditPrice.Value = p.Price;
            EditLogin.Text = p.User.Login;
        }
    }

    private async void SaveStaffClick(object? sender, RoutedEventArgs e)
    {
        if (StaffList.SelectedItem is Photographer p)
        {
            try
            {
                // === НОВАЯ ВАЛИДАЦИЯ ПЕРЕД СОХРАНЕНИЕМ ===
                string newName = EditName.Text?.Trim() ?? "";
                string newLastName = EditLastName.Text?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(newName))
                {
                    MainWindow.Instance.ShowMessage("⛔ Имя не может быть пустым!");
                    return; // Прерываем метод, ничего не сохраняем
                }

                if (string.IsNullOrWhiteSpace(newLastName))
                {
                    MainWindow.Instance.ShowMessage("⛔ Фамилия не может быть пустой!");
                    return;
                }

                using var db = Db.Create();

                // Находим свежие данные из БД
                var dbPhotographer = await db.Photographers.Include(x => x.User).FirstOrDefaultAsync(x => x.PhotographerId == p.PhotographerId);
                if (dbPhotographer == null) return;

                // Обновляем данные пользователя (теперь мы уверены, что они не пустые)
                dbPhotographer.User.Name = newName;
                dbPhotographer.User.LastName = newLastName;

                if (int.TryParse(EditPhone.Text, out int phone))
                    dbPhotographer.User.Phone = phone;

                dbPhotographer.User.Email = EditEmail.Text?.Trim() ?? "";

                // Обновляем цену
                dbPhotographer.Price = (int)(EditPrice.Value ?? 0);

                await db.SaveChangesAsync();

                MainWindow.Instance.ShowMessage("✅ Данные сотрудника успешно обновлены!");
                await LoadStaffAsync(); // Обновляем список
            }
            catch (System.Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }
    }

    private void CancelEditClick(object? sender, RoutedEventArgs e)
    {
        StaffList.SelectedItem = null;
    }
}