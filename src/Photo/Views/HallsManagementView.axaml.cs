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

public partial class HallsManagementView : UserControl
{
    private int? _currentHallId = null;

    public HallsManagementView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadHallsAsync();
    }

    private async Task LoadHallsAsync()
    {
        using var db = Db.Create();
        var halls = await db.Halls.ToListAsync();
        HallsList.ItemsSource = halls;
    }

    private void HallSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (HallsList.SelectedItem is Hall h)
        {
            _currentHallId = h.HallId;
            FormTitle.Text = $"Редактирование: {h.Name}";
            EditHallName.Text = h.Name;
            EditHallDesc.Text = h.EquipmentDescription;
            EditHallImage.Text = h.ImagePath;
        }
        else
        {
            _currentHallId = null;
            ClearForm();
        }
    }

    private void AddHallClick(object? sender, RoutedEventArgs e)
    {
        _currentHallId = null;
        HallsList.SelectedItem = null;
        FormTitle.Text = "Добавление нового зала";
        ClearForm();
    }

    private void ClearForm()
    {
        EditHallName.Text = "";
        EditHallDesc.Text = "";
        EditHallImage.Text = "";
    }

    private async void SaveHallClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            using var db = Db.Create();

            if (_currentHallId.HasValue)
            {
                // Редактирование
                var hall = await db.Halls.FindAsync(_currentHallId.Value);
                if (hall != null)
                {
                    hall.Name = EditHallName.Text?.Trim() ?? "";
                    hall.EquipmentDescription = EditHallDesc.Text?.Trim() ?? "";
                    hall.ImagePath = EditHallImage.Text?.Trim() ?? "";
                }
            }
            else
            {
                // Добавление
                var newHall = new Hall
                {
                    Name = EditHallName.Text?.Trim() ?? "Новый зал",
                    EquipmentDescription = EditHallDesc.Text?.Trim() ?? "",
                    ImagePath = EditHallImage.Text?.Trim() ?? "default.png"
                };
                db.Halls.Add(newHall);
            }

            await db.SaveChangesAsync();
            MainWindow.Instance.ShowMessage("Зал сохранен!");
            await LoadHallsAsync();
        }
        catch (System.Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void DeleteHallClick(object? sender, RoutedEventArgs e)
    {
        if (!_currentHallId.HasValue) return;

        try
        {
            using var db = Db.Create();
            var hall = await db.Halls.FindAsync(_currentHallId.Value);
            if (hall != null)
            {
                // Проверка: есть ли активные заказы на этот зал?
                bool hasOrders = await db.Orders.AnyAsync(o => o.HallId == _currentHallId.Value);
                if (hasOrders)
                {
                    MainWindow.Instance.ShowMessage("Нельзя удалить зал, на который есть заказы.");
                    return;
                }

                db.Halls.Remove(hall);
                await db.SaveChangesAsync();
                MainWindow.Instance.ShowMessage("Зал удален.");
                await LoadHallsAsync();
                ClearForm();
            }
        }
        catch (System.Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }
}