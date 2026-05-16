/** Employee record as returned by the API */
export interface Employee {
  id: number;
  employeeCode: string;           // Unique employee code (e.g., EMP000001)
  name: string;
  email: string;
  phone: string;
  departmentId: number | null;    // FK to Department
  departmentName?: string;        // Resolved department name (joined in API response)
  designation: string;
  joinDate: string;
  salary: number;
  isActive: boolean;
  icPassport: string;             // IC or Passport number — shown as NRIC on payslip
  taxNumber: string;              // Tax Identification Number (TIN)
  bankId: number | null;          // FK to BankMaster
  accountNumber: string;          // Bank account number for salary
  bankName?: string;              // Resolved bank name (joined in API response)
  profilePicture: string;         // Profile picture URL or path
}

/** Payload sent when creating or updating an employee */
export interface EmployeeRequest {
  name: string;
  email: string;
  phone: string;
  departmentId: number | null;    // Changed from department string to departmentId FK
  designation: string;
  joinDate: string;
  salary: number;
  isActive: boolean;
  icPassport: string;
  taxNumber: string;
  bankId: number | null;
  accountNumber: string;
  profilePicture: string;         // Profile picture URL or path
}
