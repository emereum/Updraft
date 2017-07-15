using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Updraft
{
    public class Config
    {
        public string DynamicIpCheckUrl { get; set; }
        public List<SecurityGroupConfig> SecurityGroups { get; } = new List<SecurityGroupConfig>();

        public static Config Read(string path = "updraft-config.json")
        {
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))
                : null;
        }

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        public static void CreateIfNotExists()
        {
            if (File.Exists("updraft-config.json"))
            {
                return;
            }

            var template = new Config
            {
                DynamicIpCheckUrl = "https://api.ipify.org",
                SecurityGroups =
                {
                    new SecurityGroupConfig
                    {
                        AccessKey = "your-access-key",
                        SecretKey = "your-secret-key",
                        VpcId = "your-vpc-id",
                        RegionEndpoint = "ap-southeast-2",
                        SecurityGroupName = "Updraft",
                        IpPermissions =
                        {
                            new IpPermissionTemplate
                            {
                                FromPort = 22,
                                ToPort = 22,
                                IpProtocol = "tcp"
                            },

                            new IpPermissionTemplate
                            {
                                FromPort = 3389,
                                ToPort = 3389,
                                IpProtocol = "tcp"
                            }
                        }
                    }
                }
            };

            File.WriteAllText("updraft-config.json", JsonConvert.SerializeObject(template, Formatting.Indented));
        }
    }
}
