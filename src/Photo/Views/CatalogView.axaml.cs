using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Photo.Services;
using static System.Collections.Specialized.BitVector32;



namespace Photo.Views
{
    public partial class CatalogView : UserControl
    {

        // ВСЕ карточки, загруженные из БД. Фильтры каждый раз начинают с полного списка,
        // поэтому очистка поиска возвращает скрытые карточки.
        private List<CourseCard> allCards = new();

        public CatalogView()
        {
            InitializeComponent();

            SortBox.SelectedIndex = 0; // по умолчанию — «Цена по возрастанию»
            Loaded += async (s, e) => await LoadCardsAsync();


        }


        // Загрузить карточки из БД и заполнить список категорий.
        private async Task LoadCardsAsync()
        {
            try
            {
                // Запоминаем выбранную категорию, чтобы после обновления она осталась.
                string? oldCategory = CategoryBox.SelectedItem as string;

                allCards = await CatalogService.LoadCardsAsync();

                // Категории: первая строка «Все категории», дальше — без повторов, по алфавиту.
                var categories = new List<string> { "Все категории" };
                categories.AddRange(allCards.Select(c => c.Category).Distinct().OrderBy(c => c));
                CategoryBox.ItemsSource = categories;

                if (oldCategory != null && categories.Contains(oldCategory))
                    CategoryBox.SelectedItem = oldCategory;
                else
                    CategoryBox.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MainWindow.Instance.ShowError(ex);
            }
        }


        // Поиск + категория + сортировка. Вызывается при любом изменении фильтров.
        private void ApplyFilters()
        {
            string text = (SearchBox.Text ?? "").Trim();
            // 1) Поиск: название ИЛИ код содержит текст (регистр букв не важен).
            IEnumerable<CourseCard> rows = allCards.Where(c =>
                c.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Contains(text, StringComparison.OrdinalIgnoreCase));
            // 2) Категория: индекс 0 — «Все категории», тогда не фильтруем.
            if (CategoryBox.SelectedIndex > 0)
            {
                string category = (string)CategoryBox.SelectedItem!;
                rows = rows.Where(c => c.Category == category);
            }
            // 3) Сортировка по цене: индекс 1 — по убыванию, иначе по возрастанию.
            if (SortBox.SelectedIndex == 1)
                rows = rows.OrderByDescending(c => c.Price);
            else
                rows = rows.OrderBy(c => c.Price);
            // 4) Показать результат.
            List<CourseCard> result = rows.ToList();
            CardsList.ItemsSource = result;
            CountText.Text = $"Найдено: {result.Count} из {allCards.Count}";
        }



        // Изменился текст поиска.
        private void SearchChanged(object? sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }
        // Изменилась категория или сортировка.
        private void FilterChanged(object? sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }
        // Кнопка «Обновить».
        private async void ReloadClick(object? sender, RoutedEventArgs e)
        {
            await LoadCardsAsync();
        }
        // Пользователь выбрал карточку — открываем страницу услуги.
        private void CardSelected(object? sender, SelectionChangedEventArgs e)
        {
            CourseCard? card = CardsList.SelectedItem as CourseCard;
            if (card == null)
                return; // выбор сброшен — ничего не делаем
            if (Session.IsGuest)
            {
                CardsList.SelectedItem = null;
                MainWindow.Instance.ShowMessage("Чтобы оформить заказ, войдите в систему.");
                return;
            }
            // Создаём страницу услуги и передаём ей номер выбранной услуги.
            MainWindow.Instance.OpenPage(new CourseView { CourseId = card.Id });
        }









    }
}