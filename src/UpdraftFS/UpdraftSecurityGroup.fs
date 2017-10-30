module UpdraftSecurityGroup

open Amazon.EC2
open Amazon.EC2.Model
open System

// todo: logging
type UpdraftSecurityGroup =
    { client: AmazonEC2Client
      securityGroupName: string
      vpcId: string }

let create client securityGroupName vpcId =
    { client = client
      securityGroupName = securityGroupName
      vpcId = vpcId }

let getSecurityGroup (updraftSecurityGroup: UpdraftSecurityGroup) =
    let request = new DescribeSecurityGroupsRequest ()
    request.Filters.Add(new Filter (Name = "group-name", Values = new System.Collections.Generic.List<string>([updraftSecurityGroup.securityGroupName])))
    request.Filters.Add(new Filter (Name = "vpc-id", Values = new System.Collections.Generic.List<string>([updraftSecurityGroup.vpcId])))
    updraftSecurityGroup.client
        .DescribeSecurityGroups(request)
        .SecurityGroups
        |> Seq.tryHead

let addIngressPermission (updraftSecurityGroup: UpdraftSecurityGroup) permission =
    match getSecurityGroup updraftSecurityGroup with
    | Some securityGroup ->
        let request = new AuthorizeSecurityGroupIngressRequest ( GroupId = securityGroup.GroupId )
        request.IpPermissions.Add(permission)
        try
            updraftSecurityGroup.client.AuthorizeSecurityGroupIngress(request) |> ignore
        with
        // If the ingress rule already exists then don't complain. The
        // user might have created it manually or they deleted their
        // .last-config or .last-ip files.
        | :? AmazonEC2Exception as e when e.ErrorCode.Equals("InvalidPermission.Duplicate") -> () // todo: logging
    | None -> raise (ApplicationException "Can't get security group to which I want to add the ingress permission")

let addIngressPermissions updraftSecurityGroup permissions =
    List.iter (addIngressPermission updraftSecurityGroup) permissions

let removeIngressPermission updraftSecurityGroup permission =
    match getSecurityGroup updraftSecurityGroup with
    | Some securityGroup ->
        let request = new RevokeSecurityGroupIngressRequest ( GroupId = securityGroup.GroupId )
        request.IpPermissions.Add(permission)
        try
            updraftSecurityGroup.client.RevokeSecurityGroupIngress(request) |> ignore
        with
        // If the ingress rule didn't exist then don't complain.
        | :? AmazonEC2Exception as e when e.ErrorCode.Equals("InvalidPermission.NotFound") -> () // todo: logging
        // If the group which should contain this rule does not
        // exist then don't complain.
        | :? AmazonEC2Exception as e when e.ErrorCode.Equals("InvalidGroup.NotFound") -> () // todo: logging
    | None -> raise (ApplicationException "Can't get security group from which I want to remove the ingress permission")

let removeIngressPermissions updraftSecurityGroup permissions =
    List.iter (removeIngressPermission updraftSecurityGroup) permissions

let createSecurityGroup (updraftSecurityGroup: UpdraftSecurityGroup) =
    let request = new CreateSecurityGroupRequest(updraftSecurityGroup.securityGroupName, "Maintained by Updraft", VpcId = updraftSecurityGroup.vpcId)
    updraftSecurityGroup.client.CreateSecurityGroup request |> ignore

let createSecurityGroupIfNotExists updraftSecurityGroup = 
    match getSecurityGroup updraftSecurityGroup with
    | Some _ -> ()
    | None -> createSecurityGroup updraftSecurityGroup
