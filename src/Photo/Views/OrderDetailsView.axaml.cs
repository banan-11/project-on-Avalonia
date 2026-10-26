using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Photo.Views
{
    public partial class OrderDetailsView : UserControl
    {


        // Номер открытого заказа. Задаётся при создании страницы:
        //     new OrderDetailsView { OrderId = 7 }
        public int OrderId { get; set; }



        public OrderDetailsView()
        {
            InitializeComponent();
        }
    }
}