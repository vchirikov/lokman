/**
 * @fileoverview gRPC-Web generated client stub for lokman
 * @enhanceable
 * @public
 */

// GENERATED CODE -- DO NOT EDIT!


/* eslint-disable */
// @ts-nocheck


import * as grpcWeb from 'grpc-web';

import * as google_api_annotations_pb from './google/api/annotations_pb';
import * as google_protobuf_duration_pb from 'google-protobuf/google/protobuf/duration_pb';
import * as google_protobuf_timestamp_pb from 'google-protobuf/google/protobuf/timestamp_pb';
import * as google_protobuf_empty_pb from 'google-protobuf/google/protobuf/empty_pb';

import {
  LockInfoResponse,
  LockRequest,
  LockResponse} from './lokman_pb';

export class DistributedLockServiceClient {
  client_: grpcWeb.AbstractClientBase;
  hostname_: string;
  credentials_: null | { [index: string]: string; };
  options_: null | { [index: string]: string; };

  constructor (hostname: string,
               credentials?: null | { [index: string]: string; },
               options?: null | { [index: string]: string; }) {
    if (!options) options = {};
    if (!credentials) credentials = {};
    options['format'] = 'text';

    this.client_ = new grpcWeb.GrpcWebClientBase(options);
    this.hostname_ = hostname;
    this.credentials_ = credentials;
    this.options_ = options;
  }

  methodInfoLock = new grpcWeb.AbstractClientBase.MethodInfo(
    LockResponse,
    (request: LockRequest) => {
      return request.serializeBinary();
    },
    LockResponse.deserializeBinary
  );

  lock(
    request: LockRequest,
    metadata: grpcWeb.Metadata | null): Promise<LockResponse>;

  lock(
    request: LockRequest,
    metadata: grpcWeb.Metadata | null,
    callback: (err: grpcWeb.Error,
               response: LockResponse) => void): grpcWeb.ClientReadableStream<LockResponse>;

  lock(
    request: LockRequest,
    metadata: grpcWeb.Metadata | null,
    callback?: (err: grpcWeb.Error,
               response: LockResponse) => void) {
    if (callback !== undefined) {
      return this.client_.rpcCall(
        new URL('/lokman.DistributedLockService/Lock', this.hostname_).toString(),
        request,
        metadata || {},
        this.methodInfoLock,
        callback);
    }
    return this.client_.unaryCall(
    this.hostname_ +
      '/lokman.DistributedLockService/Lock',
    request,
    metadata || {},
    this.methodInfoLock);
  }

  methodInfoGetLockInfo = new grpcWeb.AbstractClientBase.MethodInfo(
    LockInfoResponse,
    (request: google_protobuf_empty_pb.Empty) => {
      return request.serializeBinary();
    },
    LockInfoResponse.deserializeBinary
  );

  getLockInfo(
    request: google_protobuf_empty_pb.Empty,
    metadata: grpcWeb.Metadata | null): Promise<LockInfoResponse>;

  getLockInfo(
    request: google_protobuf_empty_pb.Empty,
    metadata: grpcWeb.Metadata | null,
    callback: (err: grpcWeb.Error,
               response: LockInfoResponse) => void): grpcWeb.ClientReadableStream<LockInfoResponse>;

  getLockInfo(
    request: google_protobuf_empty_pb.Empty,
    metadata: grpcWeb.Metadata | null,
    callback?: (err: grpcWeb.Error,
               response: LockInfoResponse) => void) {
    if (callback !== undefined) {
      return this.client_.rpcCall(
        new URL('/lokman.DistributedLockService/GetLockInfo', this.hostname_).toString(),
        request,
        metadata || {},
        this.methodInfoGetLockInfo,
        callback);
    }
    return this.client_.unaryCall(
    this.hostname_ +
      '/lokman.DistributedLockService/GetLockInfo',
    request,
    metadata || {},
    this.methodInfoGetLockInfo);
  }

}

