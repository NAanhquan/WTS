using System;
using System.Collections.Generic;

namespace AttendenceTrackingSystem.Models;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public int UserId { get; set; }

    public string? Description { get; set; }

    public DateOnly? DateFiled { get; set; }

    public string? Status { get; set; }

    public virtual User User { get; set; } = null!;
}
