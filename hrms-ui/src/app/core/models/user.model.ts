/** System user as returned by the API */
export interface AppUser {
  id: number;
  username: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

/** Payload for creating a new user */
export interface UserRequest {
  username: string;
  email: string;
  password: string;
  role: string;
  isActive: boolean;
}

/** Payload for updating an existing user */
export interface UserUpdateRequest {
  username: string;
  email: string;
  role: string;
  isActive: boolean;
  password?: string;  // optional — omit to keep existing password
}
