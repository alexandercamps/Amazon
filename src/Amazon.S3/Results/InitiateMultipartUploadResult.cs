﻿#nullable disable

using System.Xml.Serialization;

using Carbon.Storage;

namespace Amazon.S3
{
    [XmlRoot(Namespace = S3Client.Namespace)]
    public class InitiateMultipartUploadResult : IUpload
    {
        [XmlElement]
        public string Bucket { get; set; }

        [XmlElement]
        public string Key { get; set; }

        [XmlElement]
        public string UploadId { get; set; }

        #region IUpload

        string IUpload.BucketName => Bucket;

        string IUpload.ObjectName => Key;

        #endregion

        public static InitiateMultipartUploadResult ParseXml(string xmlText)
        {
            return ResponseHelper<InitiateMultipartUploadResult>.ParseXml(xmlText);
        }
    }
}

/*
<?xml version="1.0" encoding="UTF-8"?>
<InitiateMultipartUploadResult xmlns="http://s3.amazonaws.com/doc/2006-03-01/">
  <Bucket>example-bucket</Bucket>
  <Key>example-object</Key>
  <UploadId>VXBsb2FkIElEIGZvciA2aWWpbmcncyBteS1tb3ZpZS5tMnRzIHVwbG9hZA</UploadId>
</InitiateMultipartUploadResult>
*/
