using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NLog;

namespace Updraft
{
    public static class PublicIp
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string GetCurrent(Config config)
        {
            using (var client = new WebClient())
            {
                string ipAddress;

                try
                {
                    ipAddress = client.DownloadString(config.DynamicIpCheckUrl).Trim();
                }
                catch (WebException e)
                {
                    logger.Info(e, "Could not check IP address");
                    return null;
                }

                if (!Regex.IsMatch(ipAddress, "[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}"))
                {
                    throw new ApplicationException($"Expected a response that looks like an IP address from {config.DynamicIpCheckUrl} but got {ipAddress}");
                }

                return ipAddress;
            }
        }

        public static string GetLast()
        {
            try
            {
                return File.ReadAllText(".last-ip");
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static void SetLast(string last)
        {
            File.WriteAllText(".last-ip", last);
        }
    }
}
