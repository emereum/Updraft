<p align="center">
    <img src="https://user-images.githubusercontent.com/24802844/28235528-ec9475a0-6952-11e7-9cf2-9d183bfd545a.png"/><br />
    <a href="https://github.com/hoflogic/Updraft/releases/latest">Download</a>

</p>

# Updraft for AWS

(Not an official Amazon AWS product)

Updraft updates your AWS inbound security group rules whenever your public IP changes. It's like dynamic DNS for security groups.

## When to use Updraft

* When you have a dynamic IP, and
* you use security groups in AWS to permit access to resources from a specific development IP address, and
* you have at least one Windows machine on your network (Updraft is a Windows Service), and
* you can create an IAM user.

## How Updraft works

* It polls https://api.ipify.org every minute.
* If it detects that your IP has changed it connects to your AWS account via the SDK and updates one or more security groups with the IP access rules you define in `updraft-config.json` with your new public IP.

Updraft stores your previous IP address and previous configuration. It uses this previous information to remove old IP access rules. This allows Updraft rules to coexist with other IP access rules even in the same security group without leaving cruft behind as your ip address changes.

## How to use Updraft

* Create a new user with no permissions in IAM with programmatic access. Note down the access and secret keys.
* Add the following inline policy (or something more restrictive) via IAM -> Users -> (the user you created) -> Permissions -> Add inline policy -> Custom policy:

```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "updraftpolicy",
            "Effect": "Allow",
            "Action": [
                "ec2:DescribeSecurityGroups",
                "ec2:CreateSecurityGroup",
                "ec2:AuthorizeSecurityGroupIngress",
                "ec2:RevokeSecurityGroupIngress"
            ],
            "Resource": [
                "*"
            ]
        }
    ]
}
```

* [Download Updraft](https://github.com/hoflogic/Updraft/releases/latest) and extract it noting that if you want to install it as a service it shouldn't be moved.
* Run Updraft once to generate a template `updraft-config.json` file.
* Update `updraft-config.json` like this with your access and secret keys, VPC ID, region endpoint, desired security group name and ip address rules:

```
{
  "DynamicIpCheckUrl": "https://api.ipify.org",
  "SecurityGroups": [
    {
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "VpcId": "your-vpc-id",  // e.g. vpc-01234567
      "RegionEndpoint": "ap-southeast-2",
      "SecurityGroupName": "Updraft", // Updraft will create this group if it doesn't exist
      "IpPermissions": [
        {
          "FromPort": 22,
          "ToPort": 22,
          "IpProtocol": "tcp"
        },
        {
          "FromPort": 3389,
          "ToPort": 3389,
          "IpProtocol": "tcp"
        }
      ]
    }
  ]
}
```

* Run `Updraft` once again on the command line to ensure it's working (if there are no exceptions it's working)
* Run the following on an administrative command line to install and start Updraft as a service:

```
Updraft install
Updraft start
```

* Log into your AWS console and assign the security group to any EC2 instances you wish to access. If you entered a new security group name into `updraft-config.json` Updraft will have created it for you.

* (Optional) run `Updraft help` for more information about registering the service. [Topshelf](http://topshelf-project.com/) is used for service registration.
* (Optional) Configure [NLog.config](https://github.com/nlog/NLog/wiki/File-target). Information will be logged to `updraft.log` by default and the log will not be archived or trimmed.