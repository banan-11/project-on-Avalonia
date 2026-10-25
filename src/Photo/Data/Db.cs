using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photo.Data
{
    // Db — «фабрика подключений». В любом месте программы пишем:
    //     using var db = Db.Create();
    // и получаем готовый к работе CollegeContext (его создал scaffold).
    public static class Db
    {

        public static CollegeContext Create()
        {
            // connection.txt копируется рядом с .exe (настройка в .csproj, раздел 2.2).
            string path = Path.Combine(AppContext.BaseDirectory, "connection.txt");

            // Читаем строку подключения; Trim убирает лишние пробелы и переводы строк.
            string connection = File.ReadAllText(path).Trim();

            // Говорим EF Core: работаем с SQL Server по этой строке подключения.
            var options = new DbContextOptionsBuilder<CollegeContext>().UseSqlServer(connection).Options;

            // Контекст — объект, через который читаем и меняем таблицы БД.
            return new CollegeContext(options);
        }



    }

}
