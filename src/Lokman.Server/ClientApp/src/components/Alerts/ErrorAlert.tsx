import React from 'react';

interface Props {
  error?: string | null;
  errorTitle?: string;
}

export const ErrorAlert: React.FC<Props> = ({ error, errorTitle }: Props) =>
  (error) ? (
    <div class="w-full lg:w-1/2 bg-red-200 border-t-8 border-red-500 rounded-b text-red-900 px-4 py-3 shadow-md" role="alert">
      <div class="flex">
        <div class="text-3xl">
          <i class="fa fa-exclamation text-red-500 " aria-hidden="true"></i>
        </div>
        <div class="ml-4">
          <p class="font-bold">{errorTitle ?? "Error"}</p>
          <p class="text-sm">{error}</p>
        </div>
      </div>
    </div>) : null;
