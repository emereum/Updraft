using System.Collections.Generic;
using Amazon.Runtime;
using Newtonsoft.Json;

namespace Updraft
{
    public class SecurityGroupConfig
    {
        [JsonIgnore]
        public AWSCredentials AwsCredentials => new BasicAWSCredentials(AccessKey, SecretKey);
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string VpcId { get; set; }
        public string RegionEndpoint { get; set; }
        public string SecurityGroupName { get; set; }
        public List<IpPermissionTemplate> IpPermissions { get; set; } = new List<IpPermissionTemplate>();
    }
}
