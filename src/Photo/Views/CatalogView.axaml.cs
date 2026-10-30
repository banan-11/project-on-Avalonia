using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input; // Необходимо для PointerPressedEventArgs
using Photo.Services; // Для HallCard, CatalogService и Session
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Photo.Views
{
    public partial class CatalogView : UserControl
    {
        private List<HallCard> allCards = new();

        public CatalogView()
        {
            InitializeComponent();

            if (SortBox != null)
                SortBox.SelectedIndex = 0;

            // Панель админа (AdminToolbar) удалена из интерфейса.
            // Управление персоналом теперь доступно только через кнопку в MainWindow.

            Loaded += async (s, e) => await LoadCardsAsync();
        }

        // ==========================================================
        // ЗАГРУЗКА ДАННЫХ ИЗ БАЗЫ
        // ==========================================================
        private async Task LoadCardsAsync()
        {
            try
            {
                allCards = await CatalogService.LoadCardsAsync();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }

        // ==========================================================
        // ФИЛЬТРАЦИЯ И СОРТИРОВКА
        // ==========================================================
        private void ApplyFilters()
        {
            string text = (SearchBox.Text ?? "").Trim().ToLower();

            // Фильтрация по названию зала
            IEnumerable<HallCard> rows = allCards.Where(c =>
                c.Name.ToLower().Contains(text));

            // Сортировка по цене
            if (SortBox != null && SortBox.SelectedIndex == 1)
            {
                rows = rows.OrderByDescending(c => c.Price);
            }
            else
            {
                rows = rows.OrderBy(c => c.Price);
            }

            List<HallCard> result = rows.ToList();

            // Привязываем данные к ItemsControl (вместо ListBox)
            CardsList.ItemsSource = result;

            // Обновляем счетчик найденных записей
            CountText.Text = $"Найдено: {result.Count} из {allCards.Count}";
        }

        // Обработчики событий поиска и сортировки
        private void SearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilters();
        private void FilterChanged(object? sender, SelectionChangedEventArgs e) => ApplyFilters();

        // Кнопка "Обновить"
        private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadCardsAsync();

        // ==========================================================
        // ОБРАБОТЧИК КЛИКА ПО КАРТОЧКЕ (Новая логика для ItemsControl)
        // ==========================================================
        private void Card_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // В новом XAML у Border есть Tag="{Binding}", поэтому sender — это Border, а Tag — это HallCard
            if (sender is Border border && border.Tag is HallCard card)
            {
                // ПРОВЕРКА: Если это ГОСТЬ, блокируем доступ к деталям/бронированию
                if (Session.IsGuest)
                {
                    MainWindow.Instance.ShowMessage("⛔ Чтобы забронировать студию, пожалуйста, войдите в систему.");
                    return;
                }

                // Если пользователь авторизован (клиент, фотограф или админ), пускаем дальше
                var bookingPage = new CourseView
                {
                    HallId = card.Id
                };

                MainWindow.Instance.OpenPage(bookingPage);
            }
        }
    }
}