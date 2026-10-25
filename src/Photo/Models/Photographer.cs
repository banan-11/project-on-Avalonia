using System;
using System.Collections.Generic;

namespace Photo.Models;

public partial class Photographer
{
    public int PhotographerId { get; set; }

    public int Price { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
