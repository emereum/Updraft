using Amazon.EC2.Model;

namespace Updraft
{
    public class IpPermissionTemplate
    {
        public int FromPort { get; set; }
        public int ToPort { get; set; }
        public string IpProtocol { get; set; }

        public IpPermission ToAwsPermission(string ip)
        {
            return new IpPermission
            {
                FromPort = FromPort,
                ToPort = ToPort,
                IpProtocol = IpProtocol,
                IpRanges = { ip + "/32" }
            };
        }
    }
}
