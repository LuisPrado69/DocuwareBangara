using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace ValidationWebAPI.Models
{
    public class ValidationResponseModel
    {
        public ValidationResponseModel(ValidationResponseStatus status, string reason, List<string> result)
        {
            Status = status;
            Reason = reason;
            Result = result;
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public ValidationResponseStatus Status { get; set; }
        public string Reason { get; set; }
        public List<string> Result { get; set; }
    }

    public enum ValidationResponseStatus
    {
        OK,
        Failed
    }
}