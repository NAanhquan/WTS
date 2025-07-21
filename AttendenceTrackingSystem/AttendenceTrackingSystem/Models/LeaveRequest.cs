using System;
using System.Collections.Generic;

namespace AttendenceTrackingSystem.Models;

public partial class LeaveRequest
{
    public int LeaveRequestId { get; set; }

    public int UserId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public virtual User User { get; set; } = null!;
}
