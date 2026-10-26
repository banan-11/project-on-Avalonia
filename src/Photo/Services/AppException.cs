using System;
using System.Collections.Generic;
using System.Text;

namespace Photo.Services
{
    // Ошибка «для пользователя»: её текст можно сразу показать на экране.
    // AppException наследуется от стандартного класса Exception:
    // всё, что умеет Exception, умеет и она.
    public class AppException : Exception
    {
        // Конструктор передаёт текст сообщения в конструктор базового класса Exception.
        public AppException(string message) : base(message)
        {


        }
    }

}
