import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { DistributedLockServiceClient } from 'apis/LokmanServiceClientPb'
import { Empty } from 'google-protobuf/google/protobuf/empty_pb';
import { LockInfo } from 'apis/lokman_pb';
import { AppThunk } from 'app/store';

export enum LocksStateStatus {
  INITIAL,
  REQUESTED,
  LOADED,
  REQUEST_FAIL
}

export interface LocksState {
  locks: LockInfo[];
  status: LocksStateStatus;
  lastError: string | null;
}

const locksSlice = createSlice({
  name: "locks",
  initialState: { locks: [], status: LocksStateStatus.INITIAL, lastError: null } as LocksState,
  reducers: {
    getLocksSuccess: (state, action: PayloadAction<LockInfo[]>) => {
      state.locks = action.payload;
      state.status = LocksStateStatus.LOADED;
      state.lastError = null;
    },
    getLocksFailure: (state, action: PayloadAction<string>) => {
      state.lastError = action.payload;
      state.status = LocksStateStatus.REQUEST_FAIL;
    },
    getLocksStarted: state => {
      state.lastError = null;
      state.status = LocksStateStatus.REQUESTED;
    },
  },
});

export const getLocksAsync = (): AppThunk => async dispatch => {
  try {
    dispatch(locksSlice.actions.getLocksStarted());
    var grpcClient = new DistributedLockServiceClient(process.env.PUBLIC_URL);
    var response = await grpcClient.getLockInfo(new Empty(), null);
    const locks = response.getLocksList();
    dispatch(locksSlice.actions.getLocksSuccess(locks));
  }
  catch (ex) {
    if (ex as Error) {
      dispatch(locksSlice.actions.getLocksFailure(ex.message));
    }
    else {
      dispatch(locksSlice.actions.getLocksFailure("Unknown error"));
    }
  }
}

export default locksSlice;
