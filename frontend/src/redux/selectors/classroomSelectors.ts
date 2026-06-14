import { RootState } from "../store";

export const selectClassrooms          = (s: RootState) => s.classrooms.classrooms;
export const selectSelectedClassroom   = (s: RootState) => s.classrooms.selectedClassroom;
export const selectClassroomLoading    = (s: RootState) => s.classrooms.loading;
export const selectClassroomDetailLoading = (s: RootState) => s.classrooms.detailLoading;
export const selectClassroomError      = (s: RootState) => s.classrooms.error;