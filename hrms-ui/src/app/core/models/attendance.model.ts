export interface Attendance {
  id: number;
  employeeId: number;
  employeeName: string;
  date: string;
  status: 'Present' | 'Absent' | 'Leave' | 'Holiday' | 'HalfDay';
  checkIn: string | null;
  checkOut: string | null;
  workHours: number | null;
  remarks: string;
}

export interface AttendanceRequest {
  employeeId: number;
  date: string;
  status: string;
  remarks: string;
}
