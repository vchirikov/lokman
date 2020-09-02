import React from 'react';
import { LoadingStatus } from '../../types';

interface Props {
  status: LoadingStatus
}

const getStatusColor = (status: LoadingStatus): string => {
  switch (status) {
    case LoadingStatus.Fulfilled:
      return "text-green-700";
    case LoadingStatus.Rejected:
      return "text-red-700";
    default:
      return "text-grey-700"
  }
}

export const LoadingStatusInfo: React.FC<Props> = ({ status }: Props) =>
  <span class={"p-1" + getStatusColor(status)}>{LoadingStatus[status]}</span>




