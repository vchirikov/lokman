import React from 'react';
import { LoadingStatus } from 'types';

interface Props {
  status: LoadingStatus;
  updateAction: () => void;

}

export const UpdateButton: React.FC<Props> = ({ status, updateAction }: Props) => {
  let cssClasses = "form-input inline-flex items-center my-2 mx-4 px-4 py-2 border-indigo-700 bg-indigo-600 text-gray-300 rounded-lg hover:shadow-lg";
  if (status === LoadingStatus.Pending) {
    cssClasses += "cursor-not-allowed";
  }
  return (
    <button class={cssClasses} type="button" value="Update" onClick={updateAction} disabled={status === LoadingStatus.Pending}>
      {
        status === LoadingStatus.Pending ?
          <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          : null
      }
      Update
    </button>
  );
};
