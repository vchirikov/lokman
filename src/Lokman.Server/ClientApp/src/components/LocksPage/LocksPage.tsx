import React from 'react';
import { useAppDispatch, useAppSelector } from 'app/store'
import { LocksState, getLocksAsync, LocksStateStatus } from './locksSlice';

const LocksPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { locks, status, lastError } = useAppSelector<LocksState>(s => s.locksReducer);

  return <div class="bg-red-300 mt-2 p-2 lg:mt-6 flex flex-col items-center">
    <div class="bg-purple-200 w-1/4">
      <label class="block">
        <span class="p-1 text-gray-700">Status: {LocksStateStatus[status]}</span>
        <span class="bg-red-600">{lastError}</span>
        <input class="form-input mt-1 block w-full" type="button" value="Add 👝"
          onClick={() => dispatch(getLocksAsync())} />
      </label>
    </div>
    <div class="bg-orange-300 w-100">
      <ul>
{locks.map((x, i) => (<li key={i}><span>{x.getKey()} : { x.getExpiration()?.toDate().toString() }</span></li>))}
      </ul>
    </div>
  </div>
};

export default LocksPage;
