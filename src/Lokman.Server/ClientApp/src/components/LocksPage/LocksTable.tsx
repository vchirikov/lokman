import React from 'react';
import { LockInfo } from 'apis/lokman_pb';

interface Props {
  items: LockInfo[];
}

const renderItem = (item: LockInfo, index: number): JSX.Element => {
  const id = `locks_${index}`;
  const expiration = item.getExpiration();
  const datetime = (expiration) ? formatDate(expiration.toDate()) : "N/A";

  return <tr class="hover:bg-gray-100" id={id}>
    <td class="border px-4 py-2 w-9/12">{item.getKey()}</td>
    <td class="border px-4 py-2 w-2/12">{datetime}</td>
    <td class="border px-4 py-2 w-1/12 text-2xl text-purple-900">
      <a class="mx-1" href={`#${id}`} onClick={() => alert(index)}><i class="fa fa-pencil"></i></a>
      <a class="mx-1 ml-4" href={`#${id}`} onClick={() => alert(index)}><i class="fa fa-unlock"></i></a>
    </td>
  </tr>
}

const appendOneLeadingZero = (n: number): string => {
  if (n <= 9) {
    return "0" + n;
  }
  return n.toString();
}
const appendTwoLeadingZeroes = (n: number): string => {
  if (n <= 9) {
    return "00" + n;
  }
  else if (n <= 99) {
    return "00" + n;
  }
  return n.toString();
}

const formatDate = (datetime: Date): string =>
  datetime.getFullYear() + "." +
  appendOneLeadingZero(datetime.getMonth() + 1) + "." +
  appendOneLeadingZero(datetime.getDate()) + " " +
  appendOneLeadingZero(datetime.getHours()) + ":" +
  appendOneLeadingZero(datetime.getMinutes()) + ":" +
  appendOneLeadingZero(datetime.getSeconds()) + "." +
  appendTwoLeadingZeroes(datetime.getMilliseconds());

export const LocksTable: React.FC<Props> = ({ items }: Props) => {
  if (items.length > 0) {
    return <table class="table-auto">
      <thead>
        <tr>
          <th class="px-4 py-2">Key</th>
          <th class="px-4 py-2">Expiration</th>
          <th class="px-4 py-2">Actions</th>
        </tr>
      </thead>
      <tbody>
        {items.map(renderItem)}
      </tbody>
    </table>;
  }
  else {
    return <div class="flex-1 flex flex-col items-center justify-center">
      <div class="text-indigo-800 text-6xl">
        <i class="fa fa-cube" aria-hidden="true"></i>
      </div>
      <div>
        <p class="text-3xl uppercase">No data</p>
      </div>
    </div>
  }
}
