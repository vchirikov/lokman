import * as jspb from "google-protobuf"

import * as google_api_annotations_pb from './google/api/annotations_pb';
import * as google_protobuf_duration_pb from 'google-protobuf/google/protobuf/duration_pb';
import * as google_protobuf_timestamp_pb from 'google-protobuf/google/protobuf/timestamp_pb';
import * as google_protobuf_empty_pb from 'google-protobuf/google/protobuf/empty_pb';

export class LockRequest extends jspb.Message {
  getKey(): string;
  setKey(value: string): LockRequest;

  getDuration(): google_protobuf_duration_pb.Duration | undefined;
  setDuration(value?: google_protobuf_duration_pb.Duration): LockRequest;
  hasDuration(): boolean;
  clearDuration(): LockRequest;

  getToken(): number;
  setToken(value: number): LockRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LockRequest.AsObject;
  static toObject(includeInstance: boolean, msg: LockRequest): LockRequest.AsObject;
  static serializeBinaryToWriter(message: LockRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LockRequest;
  static deserializeBinaryFromReader(message: LockRequest, reader: jspb.BinaryReader): LockRequest;
}

export namespace LockRequest {
  export type AsObject = {
    key: string,
    duration?: google_protobuf_duration_pb.Duration.AsObject,
    token: number,
  }
}

export class LockResponse extends jspb.Message {
  getKey(): string;
  setKey(value: string): LockResponse;

  getToken(): number;
  setToken(value: number): LockResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LockResponse.AsObject;
  static toObject(includeInstance: boolean, msg: LockResponse): LockResponse.AsObject;
  static serializeBinaryToWriter(message: LockResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LockResponse;
  static deserializeBinaryFromReader(message: LockResponse, reader: jspb.BinaryReader): LockResponse;
}

export namespace LockResponse {
  export type AsObject = {
    key: string,
    token: number,
  }
}

export class LockInfo extends jspb.Message {
  getKey(): string;
  setKey(value: string): LockInfo;

  getIsLocked(): boolean;
  setIsLocked(value: boolean): LockInfo;

  getToken(): number;
  setToken(value: number): LockInfo;

  getExpiration(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setExpiration(value?: google_protobuf_timestamp_pb.Timestamp): LockInfo;
  hasExpiration(): boolean;
  clearExpiration(): LockInfo;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LockInfo.AsObject;
  static toObject(includeInstance: boolean, msg: LockInfo): LockInfo.AsObject;
  static serializeBinaryToWriter(message: LockInfo, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LockInfo;
  static deserializeBinaryFromReader(message: LockInfo, reader: jspb.BinaryReader): LockInfo;
}

export namespace LockInfo {
  export type AsObject = {
    key: string,
    isLocked: boolean,
    token: number,
    expiration?: google_protobuf_timestamp_pb.Timestamp.AsObject,
  }
}

export class LockInfoResponse extends jspb.Message {
  getLocksList(): Array<LockInfo>;
  setLocksList(value: Array<LockInfo>): LockInfoResponse;
  clearLocksList(): LockInfoResponse;
  addLocks(value?: LockInfo, index?: number): LockInfo;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): LockInfoResponse.AsObject;
  static toObject(includeInstance: boolean, msg: LockInfoResponse): LockInfoResponse.AsObject;
  static serializeBinaryToWriter(message: LockInfoResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): LockInfoResponse;
  static deserializeBinaryFromReader(message: LockInfoResponse, reader: jspb.BinaryReader): LockInfoResponse;
}

export namespace LockInfoResponse {
  export type AsObject = {
    locksList: Array<LockInfo.AsObject>,
  }
}

