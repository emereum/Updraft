module Config

open Amazon.EC2.Model
open Amazon.Runtime
open System.IO
open Newtonsoft.Json

type Config =
    { DynamicIpCheckUrl: string
      SecurityGroups: list<SecurityGroupConfig> }

and SecurityGroupConfig =
    { AccessKey: string
      SecretKey: string
      VpcId: string
      RegionEndpoint: string
      SecurityGroupName: string
      IpPermissions: list<IpPermissionTemplate> }

and IpPermissionTemplate =
    { FromPort: int
      ToPort: int
      IpProtocol: string }

let toAwsPermission ip template  =
    new IpPermission
        ( FromPort = template.FromPort,
          ToPort = template.ToPort,
          IpProtocol = template.IpProtocol,
          IpRanges = new System.Collections.Generic.List<string>([ip + "/32"]) )

let toAwsCredentials securityGroupConfig =
    new BasicAWSCredentials(securityGroupConfig.AccessKey, securityGroupConfig.SecretKey);

let read path =
    if File.Exists(path) then
        Some (JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)))
    else
        None

let write (config: Config) path =
    File.WriteAllText(path, JsonConvert.SerializeObject(config))

let createIfNotExists =
    if not (File.Exists("updraft-config.json")) then
        let template =
            { DynamicIpCheckUrl = "https://api.ipify.org"
              SecurityGroups =
                [ { AccessKey = "your-access-key"
                    SecretKey = "your-secret-key"
                    VpcId = "your-vpc-id"
                    RegionEndpoint = "ap-southeast-2"
                    SecurityGroupName = "Updraft"
                    IpPermissions =
                        [ { FromPort = 22
                            ToPort = 22
                            IpProtocol = "tcp" }
                          { FromPort = 3389
                            ToPort = 3389
                            IpProtocol = "tcp" }]}]}
        File.WriteAllText("updraft-config.json", JsonConvert.SerializeObject(template, Formatting.Indented))