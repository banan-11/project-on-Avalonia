using System;
using System.Collections.Generic;

namespace Photo.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public int Phone { get; set; }

    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Photographer> Photographers { get; set; } = new List<Photographer>();

    public virtual Role Role { get; set; } = null!;
}
