import React from 'react';
import { useAppDispatch, useAppSelector } from 'app/store'
import locksSlice, { LocksState } from './locksSlice';

const LocksPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { locks } = useAppSelector<LocksState>(s => s.locksReducer);

  return <div class="bg-red-300 mt-2 p-2 lg:mt-6 flex flex-col items-center">
    <div class="bg-purple-200 w-1/4">
      <label class="block">
        <span class="p-1 text-gray-700">Value: {locks}</span>
        <input class="form-input mt-1 block w-full" type="button" value="Add 👍"
          onClick={() => dispatch(locksSlice.actions.addLock())} />
      </label>
    </div>
  </div>
};

export default LocksPage;
