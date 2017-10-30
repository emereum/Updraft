module UpdraftService

open System
open Amazon.EC2
open Amazon

let getSecurityGroup config =
    let client = new AmazonEC2Client((Config.toAwsCredentials config), (RegionEndpoint.GetBySystemName config.RegionEndpoint))
    UpdraftSecurityGroup.create
        client
        config.SecurityGroupName
        config.VpcId

let cleanupOldPermissions lastIp =
    match (lastIp, Config.read(".last-config")) with
    | (Some lastIp, Some config) ->
        config.SecurityGroups
        |> Seq.iter (fun securityGroupConfig ->
            // If we have had an ip in the past it's probably in the
            // security group so remove it.
            securityGroupConfig.IpPermissions
            |> Seq.map (Config.toAwsPermission lastIp)
            |> Seq.iter (UpdraftSecurityGroup.removeIngressPermission (getSecurityGroup securityGroupConfig)))
    | _ -> ()

let applyNewPermissions (config: Config.Config) currentIp =
    config.SecurityGroups
    |> Seq.iter (fun securityGroupConfig ->
        // Add current ip address as new permissions
        securityGroupConfig.IpPermissions
        |> Seq.map (Config.toAwsPermission currentIp)
        |> Seq.iter (UpdraftSecurityGroup.addIngressPermission (getSecurityGroup securityGroupConfig)))

    // Store current ip address so we know not to hit AWS next time if
    // our ip doesn't change.
    PublicIp.setLast currentIp

    // Store current config data so we can use it next time to delete
    // the previous ip permissions. This is important because a user
    // could delete ip permissions from their config file which would
    // leave them dangling in AWS. By preserving the user's config
    // in a seperate file we ensure we are able to clean up the rules
    // we created.
    Config.write config ".last-config"

let doUpdateThunk force =
    match Config.read("updraft-config.json") with
    | None ->
        raise (InvalidOperationException("Can't do anything - updraft-config.json is gone."))
    | Some config when Seq.exists (fun (x: Config.SecurityGroupConfig) -> "your-access-key".Equals(x.AccessKey)) config.SecurityGroups ->
        raise (InvalidOperationException("The application has not been configured. Please edit updraft-config.json"))
    | Some config ->
        let currentIp = PublicIp.getCurrent config
        let lastIp = PublicIp.getLast
        match (currentIp, lastIp, force) with
        | (None, _, _) -> () // If we're offline do nothing. todo: logging
        | (Some currentIp, Some lastIp, false) when currentIp = lastIp -> () // If our IP is the same do nothing (unless forcing) todo: logging
        | (Some currentIp, lastIp, _) ->
              cleanupOldPermissions lastIp
              applyNewPermissions config currentIp

let doUpdate () =
    try
        doUpdateThunk false
    with
    | :? Exception -> () // todo: log.