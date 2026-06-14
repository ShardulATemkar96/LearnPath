import { RootState } from "../store";

export const selectPublicPaths    = (s: RootState) => s.paths.publicPaths;
export const selectMyPaths        = (s: RootState) => s.paths.myPaths;
export const selectSelectedPath   = (s: RootState) => s.paths.selectedPath;
export const selectPathLoading    = (s: RootState) => s.paths.loading;
export const selectPathDetailLoading = (s: RootState) => s.paths.detailLoading;
export const selectPathError      = (s: RootState) => s.paths.error;
