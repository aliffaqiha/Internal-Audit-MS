export interface RoleDto {
  id: string
  name: string
  description: string | null
}

export interface DepartmentDto {
  id: string
  name: string
  code: string | null
  isActive: boolean
}

export interface UserDto {
  id: string
  username: string
  email: string
  fullName: string
  isActive: boolean
  mustChangePassword: boolean
  departmentId: string | null
  departmentName: string | null
  roles: RoleDto[]
}

export interface CreateUserPayload {
  username: string
  email: string
  fullName: string
  password: string
  departmentId: string | null
  roleIds: string[]
  isActive: boolean
}

export interface UpdateUserPayload {
  email: string
  fullName: string
  departmentId: string | null
  roleIds: string[]
  isActive: boolean
  newPassword: string | null
}

export interface CreateDepartmentPayload {
  name: string
  code: string
}

export interface UpdateDepartmentPayload {
  name: string
  code: string
}