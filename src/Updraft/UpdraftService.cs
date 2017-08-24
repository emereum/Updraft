using System;
using System.Linq;
using System.Timers;
using Amazon;
using Amazon.EC2;
using NLog;

namespace Updraft
{
    /// <summary>
    /// Checks if we have a new public ip address every minute and attempts to
    /// update a AWS security groups with the new ip if it has changed. Requires
    /// the user to set up a configuration file which is created when this
    /// service starts up.
    /// </summary>
    public class UpdraftService
    {
        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(1);
        private readonly Timer timer;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public UpdraftService()
        {
            timer = new Timer(updateInterval.TotalMilliseconds)
            {
                AutoReset = true
            };

            timer.Elapsed += (sender, eventArgs) => DoUpdate();
        }

        /// <summary>
        /// http://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/how-to/ec2/security-groups.html
        /// </summary>
        private void DoUpdate()
        {
            try
            {
                DoUpdateThunk();
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        private void DoUpdateThunk(bool force = false)
        {
            var config = Config.Read("updraft-config.json");
            if (config == null)
            {
                throw new InvalidOperationException("Can't do anything - updraft-config.json is gone.");
            }

            if (config.SecurityGroups.Any(x => x.AccessKey == "your-access-key"))
            {
                throw new InvalidOperationException("The application has not been configured. Please edit updraft-config.json");
            }

            var currentIp = PublicIp.GetCurrent(config);
            var lastIp = PublicIp.GetLast();

            // If we're offline do nothing
            if (currentIp == null)
            {
                logger.Trace("Couldn't get IP address. We are probably offline.");
                return;
            }

            // If our ip is the same do nothing
            if (currentIp.Equals(lastIp))
            {
                if (force)
                {
                    logger.Info("IP hasn't changed: " + currentIp + " but we are starting up so we'll apply this IP to AWS anyway.");
                }
                else
                {
                    logger.Trace("IP hasn't changed: " + currentIp);
                    return;
                }
            }
            else
            {
                logger.Info("IP has changed from " + (lastIp ?? "nothing") + " to " + currentIp + ". Applying changes to AWS.");
            }

            CleanupOldPermissions(lastIp);

            ApplyNewPermissions(config, currentIp);
        }

        private void CleanupOldPermissions(string lastIp)
        {
            var config = Config.Read(".last-config");
            if (lastIp == null || config == null)
            {
                return;
            }

            foreach (var securityGroupConfig in config.SecurityGroups)
            {
                var securityGroup = GetSecurityGroupUsingConfig(securityGroupConfig);

                // If we have had an ip in the past it's probably in the
                // security group so remove it.
                var oldPermissions = securityGroupConfig
                    .IpPermissions
                    .Select(x => x.ToAwsPermission(lastIp));
                securityGroup.RemoveIngressPermissions(oldPermissions);
            }
        }

        private void ApplyNewPermissions(Config config, string currentIp)
        {
            foreach (var securityGroupConfig in config.SecurityGroups)
            {
                var securityGroup = GetSecurityGroupUsingConfig(securityGroupConfig);
                securityGroup.CreateSecurityGroupIfNotExists();

                // Add current ip address as new permissions
                var newPermissions = securityGroupConfig
                    .IpPermissions
                    .Select(x => x.ToAwsPermission(currentIp));
                securityGroup.AddIngressPermissions(newPermissions);
            }

            // Store current ip address so we know not to hit AWS next time if
            // our ip doesn't change.
            PublicIp.SetLast(currentIp);

            // Store current config data so we can use it next time to delete
            // the previous ip permissions. This is important because a user
            // could delete ip permissions from their config file which would
            // leave them dangling in AWS. By preserving the user's config
            // in a seperate file we ensure we are able to clean up the rules
            // we created.
            config.Write(".last-config");
        }

        private UpdraftSecurityGroup GetSecurityGroupUsingConfig(SecurityGroupConfig securityGroupConfig)
        {
            var client = new AmazonEC2Client(securityGroupConfig.AwsCredentials, RegionEndpoint.GetBySystemName(securityGroupConfig.RegionEndpoint));
            return new UpdraftSecurityGroup(
                client,
                securityGroupConfig.SecurityGroupName,
                securityGroupConfig.VpcId);
        }

        public void Start()
        {
            logger.Info(
@"
                                                                              
                                                                              
                                                                              
                                              `.-:::::-.`                     
                                           .:////////////:.                   
                                         -/////////////////:.                 
                                       `:////////////////////-                
                                     `-////-````.:////////////-               
                     ``..-----:::::///////. -///. :////////////.              
                 `..--.....---:://////////:` .:/- .////////////:-.            
           ``...----:::::::---.````.-::///////:.  :///////////////:.          
        ```````````....--::///////:-.``  ````  `.///////////////////`         
                      `-::://///////////::---:://///////////////////:         
                        `-//////////////////////////////////////////-         
                           .://////////////////////////////////////:`         
                             `-///////////////////////////////////-           
                                `-:///////////////////////////:-.             
                                    ``````` -///////////////:`                
                                             .:///////////:.                  
                                               `.-::::--.                     
                                                                              
                                                                              
                                                                              
                                                                              
Starting Updraft...

");
            Config.CreateIfNotExists();
            DoUpdateThunk(true);
            timer.Start();
        }

        public void Stop() { timer.Stop(); }
    }
}
