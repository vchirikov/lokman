import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { DistributedLockServiceClient } from 'apis/LokmanServiceClientPb';
import { Empty } from 'google-protobuf/google/protobuf/empty_pb';
import { LockInfo } from 'apis/lokman_pb';
import { LoadingStatus } from 'types';


export interface LocksState {
  locks: LockInfo[];
  status: LoadingStatus;
  lastError: string | null;
}

export const getLocksAsync = createAsyncThunk("locks/getLocksAsync", async () => {
  const grpcClient = new DistributedLockServiceClient(process.env.PUBLIC_URL);
  const response = await grpcClient.getLockInfo(new Empty(), null);
  const locks = response.getLocksList();
  return locks.filter(l => l.getIsLocked());
});

const locksSlice = createSlice({
  name: "locks",
  initialState: { locks: [], status: LoadingStatus.Initial, lastError: null } as LocksState,
  reducers: {},
  extraReducers: builder => builder
    .addCase(getLocksAsync.fulfilled, (state, action) => {
      state.locks = action.payload;
      state.status = LoadingStatus.Fulfilled;
      state.lastError = null;
    })
    .addCase(getLocksAsync.rejected, (state, action) => {
      state.lastError = action.error.message ?? "Unknown error";
      state.status = LoadingStatus.Rejected;
      state.locks = [];
    })
    .addCase(getLocksAsync.pending, (state, action) => {
      state.lastError = null;
      state.status = LoadingStatus.Pending;
    })
});


export default locksSlice;
