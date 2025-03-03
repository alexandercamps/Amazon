using System;

namespace Amazon.S3
{
    public class GetPresignedUrlRequest
    {
        public GetPresignedUrlRequest(
            string method,
            string host,
            AwsRegion region,
            string bucketName,
            string objectKey,
            TimeSpan expiresIn)
        {
            Method = method ?? throw new ArgumentException(nameof(method));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Region = region;
            BucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            Key = objectKey;
            ExpiresIn = expiresIn;
        }

        public string Method { get; }

        public string Host { get; }

        public string BucketName { get; }

        public AwsRegion Region { get; }

        public string Key { get; }

        public TimeSpan ExpiresIn { get; }
    }
}
