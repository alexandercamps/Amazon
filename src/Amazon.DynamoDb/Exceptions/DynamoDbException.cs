﻿using System;
using System.Collections.Generic;

using Amazon.Scheduling;

using Carbon.Json;

namespace Amazon.DynamoDb
{
    public class DynamoDbException : Exception, IException
    {
        public DynamoDbException(string message)
            : base(message) { }

        public DynamoDbException(string message, string? type)
          : base(message)
        {
            Type = type;
        }

        public DynamoDbException(string message, Exception innerException)
            : base(message, innerException) { }

        public DynamoDbException(string message, List<Exception> exceptions)
            : base(message)
        {
            Exceptions = exceptions;
        }

        public string? Type { get; set; }

        public int StatusCode { get; set; }

        public static DynamoDbException Parse(string jsonText)
        {
            return FromJson(JsonObject.Parse(jsonText));
        }

        public IReadOnlyList<Exception>? Exceptions { get; }

        public static DynamoDbException FromJson(JsonObject json)
        {
            string type = json["__type"];
            string message = "";

            if (json.TryGetValue("message", out var m))
            {
                message = m;
            }
            else if (json.TryGetValue("Message", out m))
            {
                message = m;
            }

            if (type != null)
            {
                int poundIndex = type.IndexOf('#');

                if (poundIndex > -1)
                {
                    type = type.Substring(poundIndex + 1);
                }
            }

            if (type == "ConditionalCheckFailedException")
            {
                return new Conflict(message);
            }

            return new DynamoDbException(message, type);
        }

        public bool IsTransient
        {
            get
            {
                // Client Errors = 4xx (Don't retry)
                // Server Errors = 5xx (Retry)

                switch (Type)
                {
                    case "InternalServerError":
                    case "InternalFailure":
                    case "ProvisionedThroughputExceededException":
                    case "ThrottlingException": return true;
                }

                return false;
            }
        }
    }
}

/*
{
  "__type":"com.amazonaws.dynamodb.v20111205#ProvisionedThroughputExceededException",
  "message":"The level of configured provisioned throughput for the table was exceeded. Consider increasing your provisioning level with the UpdateTable API"
}

{"__type":"com.amazon.coral.service#ExpiredTokenException","message":"The security token included in the request is expired"}

{"__type":"com.amazon.coral.service#SerializationException","Message":"Start of list found where not expected"}
*/
