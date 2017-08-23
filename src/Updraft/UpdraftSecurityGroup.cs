using System.Collections.Generic;
using System.Linq;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Updraft
{
    /// <summary>
    /// Wraps an AWS security group and provides some helper methods to make
    /// it easier to remove and add ip permissions to the security group.
    /// </summary>
    public class UpdraftSecurityGroup
    {
        private readonly AmazonEC2Client client;
        private readonly string securityGroupName;
        private readonly string vpcId;

        public UpdraftSecurityGroup(AmazonEC2Client client, string securityGroupName, string vpcId)
        {
            this.client = client;
            this.vpcId = !string.IsNullOrWhiteSpace(vpcId) ? vpcId : null;
            this.securityGroupName = securityGroupName;
        }

        public void AddIngressPermissions(IEnumerable<IpPermission> permissions)
        {
            foreach (var permission in permissions)
            {
                AddIngressPermission(permission);
            }
        }

        public void RemoveIngressPermissions(IEnumerable<IpPermission> permissions)
        {
            foreach (var permission in permissions)
            {
                RemoveIngressPermission(permission);
            }
        }

        public void AddIngressPermission(IpPermission permission)
        {
            var request = new AuthorizeSecurityGroupIngressRequest
            {
                GroupId = GetSecurityGroup().GroupId,
                IpPermissions = { permission }
            };

            try
            {
                client.AuthorizeSecurityGroupIngress(request);
            }
            catch (AmazonEC2Exception e)
            {
                // If the ingress rule already exists then don't complain. The
                // user might have created it manually or they deleted their
                // .last-config or .last-ip files.
                if (e.ErrorCode.Equals("InvalidPermission.Duplicate"))
                {
                    return;
                }

                throw;
            }
        }

        public void RemoveIngressPermission(IpPermission permission)
        {
            var request = new RevokeSecurityGroupIngressRequest
            {
                GroupId = GetSecurityGroup().GroupId,
                IpPermissions = { permission }
            };

            client.RevokeSecurityGroupIngress(request);
        }

        /// <summary>
        /// Attempts to retrieve the security group or create it if it doesn't
        /// exist.
        /// </summary>
        public void CreateSecurityGroupIfNotExists()
        {
            if (GetSecurityGroup() == null)
            {
                CreateSecurityGroup();
            }
        }

        private SecurityGroup GetSecurityGroup()
        {
            // Attempt to retrive the security group
            var request = new DescribeSecurityGroupsRequest
            {
                Filters = {
                    new Filter
                    {
                        Name = "group-name",
                        Values = { securityGroupName }
                    },

                    new Filter
                    {
                        Name = "vpc-id",
                        Values = { vpcId }
                    }
                }
            };

            return client
                .DescribeSecurityGroups(request)
                .SecurityGroups
                .SingleOrDefault();
        }

        private SecurityGroup CreateSecurityGroup()
        {
            var request = new CreateSecurityGroupRequest(securityGroupName, "Maintained by Updraft")
            {
                VpcId = vpcId
            };

            client.CreateSecurityGroup(request);

            return GetSecurityGroup();
        }
    }
}
