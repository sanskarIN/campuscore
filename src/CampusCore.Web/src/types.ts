export type Role = 'Administrator' | 'Registrar' | 'Teacher' | string;

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  displayName: string;
  roles: Role[];
}

export interface CurrentUser {
  id: string | null;
  name: string | null;
  roles: Role[];
}

export interface DashboardSummary {
  activeStudents: number;
  activeStaff: number;
  sections: number;
  presentToday: number;
  absentToday: number;
  pendingLeaveRequests: number;
  publishedAnnouncements: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface StudentListItem {
  id: string;
  admissionNumber: string;
  displayName: string;
  dateOfBirth: string;
  isActive: boolean;
  className: string | null;
  sectionName: string | null;
  rollNumber: string | null;
}

export interface GuardianModel {
  id: string;
  name: string;
  relationship: string;
  email: string | null;
  phone: string | null;
  isPrimary: boolean;
}

export interface EnrollmentModel {
  id: string;
  academicYearId: string;
  academicYear: string;
  sectionId: string;
  className: string;
  sectionName: string;
  rollNumber: string | null;
  status: string;
}

export interface StudentDetails {
  id: string;
  admissionNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string | null;
  phone: string | null;
  addressLine: string | null;
  isActive: boolean;
  guardians: GuardianModel[];
  enrollments: EnrollmentModel[];
}

export interface StaffMember {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  jobTitle: string;
  status: string | number;
}

export interface AnnouncementAttachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface Announcement {
  id: string;
  title: string;
  body: string;
  audience: string | number;
  publishAtUtc: string;
  expiresAtUtc: string | null;
  attachments: AnnouncementAttachment[];
}

export interface SearchResult {
  type: 'student' | 'staff';
  id: string;
  title: string;
  subtitle: string;
}

export interface InstitutionSettings {
  id: string;
  institutionName: string;
  address: string | null;
  timeZoneId: string | null;
  locale: string;
  dateFormat: string;
  defaultPageSize: number;
  allowGuardianPortal: boolean;
}

export interface AuditLog {
  id: string;
  occurredAtUtc: string;
  actorUserId: string;
  action: string;
  entityType: string;
  entityId: string;
  safeMetadataJson: string | null;
  correlationId: string | null;
}

export interface AdminUser {
  id: string;
  email: string | null;
  displayName: string;
  isActive: boolean;
  lockoutEnd: string | null;
  roles: string[];
}

export interface CreateStudentInput {
  admissionNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string | null;
  phone: string | null;
  addressLine: string | null;
}
