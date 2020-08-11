import { configureStore, Action } from '@reduxjs/toolkit';
import { ThunkAction } from 'redux-thunk';
import { useDispatch, useSelector } from 'react-redux';
import rootReducer, { AppState } from './rootReducer';

const isDevelopment = process.env.NODE_ENV !== "production";

const store = configureStore({
  reducer: rootReducer,
  devTools: isDevelopment,
});

if (isDevelopment && module.hot) {
  module.hot.accept('./rootReducer', () => {
    const newRootReducer = require('./rootReducer').default
    store.replaceReducer(newRootReducer)
  })
}

export type AppDispatch = typeof store.dispatch
export type AppThunk<ReturnType = void> = ThunkAction<ReturnType, AppState, unknown, Action<string>>

// Our typed helpers for RTK and the company
export const useAppDispatch = () => useDispatch<AppDispatch>();
export function useAppSelector<TSelected = unknown>(
  selector: (state: AppState) => TSelected,
  equalityFn?: (left: TSelected, right: TSelected) => boolean
): TSelected {
  return useSelector<AppState, TSelected>(selector, equalityFn);
}

export default store
