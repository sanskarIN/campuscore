namespace CampusCore.Domain.Enums;

public enum EnrollmentStatus { Active = 1, Graduated = 2, Transferred = 3, Withdrawn = 4 }
public enum AttendanceStatus { Present = 1, Absent = 2, Late = 3, Excused = 4 }
public enum LeaveStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }
public enum StaffStatus { Active = 1, OnLeave = 2, Inactive = 3 }
public enum AnnouncementAudience { Everyone = 1, Students = 2, Guardians = 3, Staff = 4 }
