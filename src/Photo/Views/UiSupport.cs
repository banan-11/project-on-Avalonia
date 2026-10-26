using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Photo.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Photo.Views
{
    // Вспомогательные методы для интерфейса: цвета, картинки, тексты ошибок.
    // static — объект создавать не нужно, пишем сразу UiSupport.Picture(...).
    public static class UiSupport
    {


        // Адрес папки Assets внутри программы.
        
        private const string AssetsRoot = "avares://Photo/Assets/";

        // Уже загруженные картинки: ключ — адрес файла, значение — сама картинка.
        private static readonly Dictionary<string, Bitmap> cache = new();

        // Цвет из App.axaml по имени (x:Key), например Brush("LowStockBrush").
        public static IBrush Brush(string key)
        {
            return (IBrush)Application.Current!.Resources[key]!;
        }


        // Картинка из папки Assets/Images по имени файла из столбца image_path.
        // Если имя пустое или такого файла нет — возвращаем заглушку picture.png.
        public static Bitmap Picture(string? fileName)
        {
            string uri = AssetsRoot + "Images/" + fileName;
            if (string.IsNullOrWhiteSpace(fileName) || !AssetLoader.Exists(new Uri(uri)))
            {
                uri = AssetsRoot + "picture.png";
            }

            // Если картинку уже открывали — берём из cache, иначе читаем файл.
            if (!cache.TryGetValue(uri, out Bitmap? bitmap))
            {
                using var stream = AssetLoader.Open(new Uri(uri));
                bitmap = new Bitmap(stream);
                cache[uri] = bitmap;
            }
            return bitmap;
        }



        // Превращает техническую ошибку в понятный пользователю текст.
        public static string ErrorText(Exception error)
        {
            // Наши собственные ошибки (throw new AppException("...")) — текст уже понятный.
            if (error is AppException)
                return error.Message;

            // Ошибки SQL Server и сохранения через EF Core.
            if (error is SqlException || error is DbUpdateException)
                return "Ошибка базы данных. Проверьте, что SQL Server запущен и строка " +
                       "в connection.txt верна, затем нажмите «Обновить».";

            // Не найден файл (обычно connection.txt).
            if (error is IOException)
                return "Не найден файл connection.txt рядом с программой.";

            // Всё остальное: показываем техническое описание, чтобы найти причину.
            return "Операция не выполнена: " + error.Message;
        }











    }
}
