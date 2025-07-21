using System;
using System.Collections.Generic;

namespace AttendenceTrackingSystem.Models;

public partial class EmailNotification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string? Message { get; set; }

    public DateTime? DateSent { get; set; }

    public virtual User User { get; set; } = null!;
}
