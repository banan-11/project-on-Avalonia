using System;
using System.Collections.Generic;

namespace Photo.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateOnly BookingDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public decimal HoursCount { get; set; }

    public int StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int UserId { get; set; }

    public int HallId { get; set; }

    public int PhotographerId { get; set; }

    public virtual Hall Hall { get; set; } = null!;

    public virtual Photographer Photographer { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
