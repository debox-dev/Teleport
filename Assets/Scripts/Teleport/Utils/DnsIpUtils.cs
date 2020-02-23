using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace DeBox.Teleport.Utils
{
    public static class DnsIpUtils
    {
        public static bool TryGetIp(string hostnameOrIp, out IPAddress resolved, bool allowIpV6 = false)
        {
            if (IPAddress.TryParse(hostnameOrIp, out resolved))
            {
                return true;
            }
            try
            {
                foreach (var addr in Dns.GetHostEntry(hostnameOrIp).AddressList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork || (addr.AddressFamily == AddressFamily.InterNetworkV6 && allowIpV6))
                    {
                        resolved = addr;
                        return true;
                    }
                }
                Debug.LogError("Failed to resolve a valid IP from host: " + hostnameOrIp);
                return false;
            }
            catch (SocketException e)
            {
                Debug.LogError("Failed resolving hostname: " + e.Message);
                return false;
            }
            catch (ArgumentNullException e)
            {
                Debug.LogError("Failed resolving hostname: got null hostname: " + e.Message);
                return false;
            }
            catch (ArgumentException e)
            {
                Debug.LogError("Failed resolving hostname: Invalid address: " + hostnameOrIp  + ": " + e.Message);
                return false;
            }
        }
    }
}
