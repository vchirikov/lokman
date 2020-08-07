import { createSlice } from '@reduxjs/toolkit';

export interface LocksState {
  locks: number;
}

const locksSlice = createSlice({
  name: "locks",
  initialState: { locks: 0 } as LocksState,
  reducers: {
    addLock: state => { state.locks = state.locks + 1; },
  },
});


export default locksSlice;
