﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Amazon.Scheduling;

using Carbon.Storage;

namespace Amazon.S3
{
    public sealed class S3Bucket : IBucket, IReadOnlyBucket
    {
        private readonly S3Client client;
        private readonly string bucketName;

        private static readonly RetryPolicy retryPolicy = RetryPolicy.ExponentialBackoff(
             initialDelay : TimeSpan.FromMilliseconds(100),
             maxDelay     : TimeSpan.FromSeconds(3),
             maxRetries   : 4
        );

        public S3Bucket(AwsRegion region, string bucketName, IAwsCredential credential)
            : this(bucketName, new S3Client(region, credential)) { }

        public S3Bucket(string bucketName, S3Client client)
        {
            this.bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            this.client     = client     ?? throw new ArgumentNullException(nameof(client));
        }

        public string Name => bucketName;

        public S3Bucket WithTimeout(TimeSpan timeout)
        {
            client.SetTimeout(timeout);

            return this;
        }

        public Task<IReadOnlyList<IBlob>> ListAsync(string? prefix)
        {
            return ListAsync(prefix, null, 1000);
        }

        public async Task<IReadOnlyList<IBlob>> ListAsync(string? prefix, string? continuationToken, int take = 1000)
        {
            var request = new ListBucketOptions {
                Prefix = prefix,
                ContinuationToken = continuationToken,
                MaxKeys = take
            };

            var result = await client.ListBucketAsync(bucketName, request).ConfigureAwait(false);

            return result.Items;
        }

        public async Task<IBlob> GetAsync(string key)
        {
            int retryCount = 0;
            Exception lastException;

            do
            {
                if (retryCount > 0)
                {
                    await Task.Delay(retryPolicy.GetDelay(retryCount)).ConfigureAwait(false);
                }

                try
                {
                    var request = new GetObjectRequest(
                        host       : client.Host,
                        bucketName : bucketName,
                        key        : key
                    );

                    return await client.GetObjectAsync(request).ConfigureAwait(false);
                }
                catch (S3Exception ex) when (ex.IsTransient)
                {
                    lastException = ex;
                }

                retryCount++;

            }
            while (retryPolicy.ShouldRetry(retryCount));

            throw lastException;
        }

        public string GetPresignedUrl(string key, TimeSpan expiresIn, string method = "GET")
        {
            var request = new GetPresignedUrlRequest(method, client.Host, client.Region, bucketName, key, expiresIn);

            return client.GetPresignedUrl(request);
        }

        // If-Modified-Since                    OR 304
        // If-None-Match        -- ETag         OR 304
        public async Task<IBlobResult> GetAsync(string key, GetBlobOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            int retryCount = 0;
            Exception lastException;

            do
            {
                if (retryCount > 0)
                {
                    await Task.Delay(retryPolicy.GetDelay(retryCount)).ConfigureAwait(false);
                }

                try
                {
                    var request = ConstructGetRequest(key, options);

                    return await client.GetObjectAsync(request).ConfigureAwait(false);
                }
                catch (S3Exception ex) when (ex.IsTransient)
                {
                    lastException = ex;
                }

                retryCount++;

            }
            while (retryPolicy.ShouldRetry(retryCount));

            throw lastException;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetPropertiesAsync(string key)
        {
            var request = new ObjectHeadRequest(client.Host, bucketName, key: key);

            using var result = await client.GetObjectHeadAsync(request).ConfigureAwait(false);

            return result.Properties!;
        }

        public Task<RestoreObjectResult> InitiateRestoreAsync(string key, int days)
        {
            var request = new RestoreObjectRequest(client.Host, bucketName, key) {
                Days = days
            };

            return client.RestoreObjectAsync(request);
        }

        public async Task<CopyObjectResult> PutAsync(
            string destinationKey,
            S3ObjectLocation sourceLocation,
            IReadOnlyDictionary<string, string>? metadata = null)
        { 
            if (destinationKey is null)
            {
                throw new ArgumentNullException(nameof(destinationKey));
            }
         
            int retryCount = 0;
            Exception lastException;

            do
            {
                if (retryCount > 0)
                {
                    await Task.Delay(retryPolicy.GetDelay(retryCount)).ConfigureAwait(false);
                }

                var request = new CopyObjectRequest(
                    host   : client.Host,
                    source : sourceLocation,
                    target : new S3ObjectLocation(bucketName, destinationKey)
                );

                if (metadata != null)
                {
                    request.MetadataDirective = MetadataDirectiveValue.Replace;

                    SetHeaders(request, metadata);
                }

                try
                {
                    return await client.CopyObjectAsync(request).ConfigureAwait(false);
                }
                catch (S3Exception ex) when (ex.IsTransient)
                {
                    lastException = ex;
                }

                retryCount++;
            }
            while (retryPolicy.ShouldRetry(retryCount));

            throw new S3Exception($"Error copying '{sourceLocation}' to '{destinationKey}'", lastException);
        }

        public Task PutAsync(IBlob blob)
        {
            return PutAsync(blob, new PutBlobOptions());
        }

        public async Task PutAsync(IBlob blob, PutBlobOptions options)
        {
            if (blob is null)
                throw new ArgumentNullException(nameof(blob));

            if (blob.Key is null)
                throw new ArgumentNullException("blob.Key");

            // TODO: Chunked upload

            Stream stream = await blob.OpenAsync().ConfigureAwait(false);

            #region Stream conditions

            if (stream.Length == 0)
                throw new ArgumentException("May not be empty", nameof(blob));

            // Ensure we're at the start of the stream
            if (stream.CanSeek && stream.Position != 0)
                throw new ArgumentException($"Must be 0. Was {stream.Position}.", paramName: "blob.Position");

            #endregion

            int retryCount = 0;

            Exception lastException;

            do
            {
                var request = new PutObjectRequest(client.Host, bucketName, blob.Key);

                request.SetStream(stream);

                // Server side encrpytion
                if (options.EncryptionKey != null)
                {
                    request.SetCustomerEncryptionKey(new ServerSideEncryptionKey(options.EncryptionKey));
                }

                SetHeaders(request, blob.Properties);

                try
                {
                    await client.PutObjectAsync(request).ConfigureAwait(false);

                    return;
                }
                catch (S3Exception ex) when (ex.IsTransient)
                {
                    lastException = ex;
                }
                
                stream.Position = 0; // reset the stream position
                
                retryCount++;

                await Task.Delay(retryPolicy.GetDelay(retryCount)).ConfigureAwait(false);
            }
            while (retryPolicy.ShouldRetry(retryCount));
            
            throw lastException;
        }

        public async Task DeleteAsync(string name)
        {
            await client.DeleteObjectAsync(
                new DeleteObjectRequest(client.Host, bucketName, name)
            ).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string key, string version)
        {
            var request = new DeleteObjectRequest(client.Host, bucketName, key, version);

            await client.DeleteObjectAsync(request).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string[] keys)
        {
            var batch = new DeleteBatch(keys);

            var request = new BatchDeleteRequest(client.Host, bucketName, batch);

            var result = await client.DeleteObjectsAsync(request).ConfigureAwait(false);

            if (result.HasErrors)
            {
                throw new Exception(result.Errors[0].Message);
            }

            if (result.Deleted.Length != keys.Length)
            {
                throw new Exception("Deleted count not equal to keys.Count");
            }
        }

        #region Uploads

        public async Task<IUpload> InitiateUploadAsync(string key, IReadOnlyDictionary<string, string> properties)
        {
            var request = new InitiateMultipartUploadRequest(client.Host, bucketName, key) {
                Content = new StringContent(string.Empty)
            };

            SetHeaders(request, properties);

            return await client.InitiateMultipartUploadAsync(request).ConfigureAwait(false);
        }

        public async Task<IUploadBlock> UploadPartAsync(IUpload upload, int number, Stream stream)
        {
            if (upload is null) throw new ArgumentNullException(nameof(upload));
            
            var request = new UploadPartRequest(
                host       : client.Host,
                bucketName : upload.BucketName,
                key        : upload.ObjectName,
                uploadId   : upload.UploadId,
                partNumber : number
            );

            request.SetStream(stream);

            return await client.UploadPartAsync(request).ConfigureAwait(false);
        }

        public async Task FinalizeUploadAsync(IUpload upload, IUploadBlock[] blocks)
        {
            var request = new CompleteMultipartUploadRequest(client.Host, upload, blocks);

            await client.CompleteMultipartUploadAsync(request).ConfigureAwait(false);
        }

        public async Task CancelUploadAsync(IUpload upload)
        {
            if (upload is null) throw new ArgumentNullException(nameof(upload));

            var request = new AbortMultipartUploadRequest(
                host       : client.Host, 
                bucketName : upload.BucketName, 
                key        : upload.ObjectName,
                uploadId   : upload.UploadId
            );

            await client.AbortMultipartUploadAsync(request).ConfigureAwait(false);
        }

        #endregion

        #region Helpers

        private GetObjectRequest ConstructGetRequest(string key, GetBlobOptions options)
        {
            var request = new GetObjectRequest(
                host        : client.Host,
                bucketName  : bucketName,
                key         : key
            );

            if (options.IfModifiedSince is DateTimeOffset ifModifiedSince)
            {
                request.IfModifiedSince = ifModifiedSince;
            }

            if (options.EncryptionKey is byte[] encryptionKey)
            {
                request.SetCustomerEncryptionKey(new ServerSideEncryptionKey(encryptionKey));
            }

            if (options.IfNoneMatch is string ifNoneMatch)
            {
                request.IfNoneMatch = ifNoneMatch;
            }

            if (options.Range is ByteRange range)
            {
                request.SetRange(range.Start, range.End);
            }

            if (options.BufferResponse)
            {
                request.CompletionOption = HttpCompletionOption.ResponseContentRead;
            }

            return request;
        }

        private static void SetHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string> headers)
        {
            if (headers is null) return;

            foreach (var item in headers)
            {
                switch (item.Key)
                {
                    case "Content-Encoding" :
                        request.Content.Headers.ContentEncoding.Add(item.Value);
                        break;
                    case "Content-Type":
                        if (request.Content is null)
                        {
                            request.Content = new ByteArrayContent(Array.Empty<byte>());
                        }

                        request.Content.Headers.ContentType = new MediaTypeHeaderValue(item.Value);

                        break;

                    // Skip list...
                    case "Accept-Ranges":
                    case "Content-Length":
                    case "Date":
                    case "ETag":
                    case "Server":
                    case "Last-Modified":
                    case "x-amz-id-2":
                    case "x-amz-expiration":
                    case "x-amz-request-id2":
                    case "x-amz-request-id":
                        break;

                    default:
                        request.Headers.Add(item.Key, item.Value);

                        break;
                }
            }
        }

        #endregion
    }
}

// NOTES:
// As of PR #19082, HttpClient no longer disposes the HttpContent request object.
// https://github.com/dotnet/corefx/issues/21790