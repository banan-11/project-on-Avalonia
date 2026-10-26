using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Photo.Views
{
    public partial class CourseView : UserControl
    {
        // Номер открытой услуги. Задаётся при создании страницы:
        //     new CourseView { CourseId = 5 }
        public int CourseId { get; set; }

        public CourseView()
        {
            InitializeComponent();
        }
    }
}