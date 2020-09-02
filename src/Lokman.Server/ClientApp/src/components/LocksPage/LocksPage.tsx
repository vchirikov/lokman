import { UpdateButton } from './UpdateButton';
import React from 'react';
import { useAppDispatch, useAppSelector } from 'app/store';
import { LocksState, getLocksAsync } from './locksSlice';
import { ErrorAlert } from 'components/Alerts/ErrorAlert';
import { LoadingStatusInfo } from './LoadingStatusInfo';
import { LocksTable } from './LocksTable';

const LocksPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { locks, status, lastError } = useAppSelector<LocksState>(s => s.locksReducer);

  return <div class="px-2 py-1 my-4 mb-4 flex-1 flex flex-col items-center">
    <ErrorAlert errorTitle="Can't get the locks information" error={lastError} />
    <div class="w-full mt-3 bg-white rounded-md shadow-md flex flex-col flex-1 flex-grow content-between">
      <div class="border-b-2 border-indigo-400 flex flex-row-reverse flex-no-wrap justify-between align-middle">
        <UpdateButton status={status} updateAction={() => dispatch(getLocksAsync())} />
        <button class="form-input text-sm items-center my-2 mx-4 block px-6 py-2 border-green-700 bg-green-600 text-gray-300 rounded-lg hover:shadow-lg" >
          <i class="fa fa-plus -ml-1 mr-1" aria-hidden="true"></i><span class="text-base">Add</span>
        </button>
      </div>
      <div class="flex-1 flex flex-col">
        <LocksTable items={locks} />
      </div>
      <div class="border-t-2 border-indigo-400 h-7 p-1">
        <span class="p-1"><i class="fa fa-info text-indigo-600" aria-hidden="true"></i> <LoadingStatusInfo status={status} /></span>
      </div>
    </div>
  </div>;
};

export default LocksPage;
