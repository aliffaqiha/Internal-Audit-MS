import { api, type PagedResult } from "@/lib/api"

import type {
  CreateDepartmentPayload,
  CreateUserPayload,
  DepartmentDto,
  RoleDto,
  UpdateDepartmentPayload,
  UpdateUserPayload,
  UserDto,
} from "./types"

export interface UsersQuery {
  search?: string
  departmentId?: string
  roleId?: string
  isActive?: boolean
  page?: number
  pageSize?: number
}

export const adminApi = {
  roles: () => api.get<RoleDto[]>("/roles").then((r) => r.data),

  departments: () => api.get<DepartmentDto[]>("/departments").then((r) => r.data),
  createDepartment: (payload: CreateDepartmentPayload) =>
    api.post<GuidResponse>("/departments", payload).then((r) => r.data.id),
  updateDepartment: (id: string, payload: UpdateDepartmentPayload) =>
    api.put(`/departments/${id}`, payload).then(() => undefined),
  deleteDepartment: (id: string) => api.delete(`/departments/${id}`).then(() => undefined),

  users: (params?: UsersQuery) =>
    api.get<PagedResult<UserDto>>("/users", { params }).then((r) => r.data),
  createUser: (payload: CreateUserPayload) =>
    api.post<GuidResponse>("/users", payload).then((r) => r.data.id),
  updateUser: (id: string, payload: UpdateUserPayload) =>
    api.put(`/users/${id}`, payload).then(() => undefined),
  deleteUser: (id: string) => api.delete(`/users/${id}`).then(() => undefined),
}

interface GuidResponse {
  id: string
}