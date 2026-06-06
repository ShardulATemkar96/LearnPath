import { createSlice } from "@reduxjs/toolkit";
import { LearningPath } from "../../types/path.types";

interface PathState {
  paths: LearningPath[];
  selectedPath: LearningPath | null;
  loading: boolean;
  error: string | null;
}

const initialState: PathState = {
  paths: [],
  selectedPath: null,
  loading: false,
  error: null,
};

const pathSlice = createSlice({
  name: "paths",
  initialState,
  reducers: {},
});

export default pathSlice.reducer;