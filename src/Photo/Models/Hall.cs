using System;
using System.Collections.Generic;

namespace Photo.Models;

public partial class Hall
{
    public int HallId { get; set; }

    public string Name { get; set; } = null!;

    public string? ImagePath { get; set; }

    public string? EquipmentDescription { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
