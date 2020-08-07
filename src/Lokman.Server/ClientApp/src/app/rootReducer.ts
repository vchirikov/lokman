import { combineReducers } from '@reduxjs/toolkit';
import locksSlice from 'components/LocksPage/locksSlice';

const rootReducer = combineReducers({
  locksReducer: locksSlice.reducer,
});

export type AppState = ReturnType<typeof rootReducer>;
export default rootReducer;
