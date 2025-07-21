using System;
using System.Collections.Generic;

namespace AttendenceTrackingSystem.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int UserId { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public virtual User User { get; set; } = null!;
}
