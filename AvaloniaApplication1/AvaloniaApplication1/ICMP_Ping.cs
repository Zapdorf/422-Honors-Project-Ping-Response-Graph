using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace AvaloniaApplication1
{

    public class ICMP_Ping
    {
        private static ushort sequenceNumber = 0;
        private static IPAddress ipAddress;


        public bool SetNewAddress(string host){
            try
            {
                ipAddress = Dns.GetHostEntry(host).AddressList.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
            }
            catch (Exception ex)
            {
                try
                {
                    ipAddress = IPAddress.Parse(host);
                }
                catch (Exception e)
                {
                    return false;
                }


                //return -1;
                //return $"Failed to resolve host: {ex.Message}";
            }
            return true;
        }

        public double SendPing()
        {
            
            Socket icmpSocket;
            try
            {
                icmpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            }
            catch (Exception ex)
            {
                return -2;
                // permission error
            }
            
            int timeout = 1000; // Timeout in milliseconds
            icmpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
            

            // header = struct.pack("bbHHh", ICMP_ECHO_REQUEST, 0, myChecksum, ID, 1)
            byte[] buffer = new byte[1024]; // Packet buffer

            // Create ICMP Echo Request
            buffer[0] = 0x08; // ICMP Type (8 for Echo Request)
            buffer[1] = 0x00; // ICMP Code (0)
            buffer[2] = 0x00; // ICMP Checksum (set later)
            buffer[3] = 0x00; // ICMP Checksum (set later)

            // ID (2 bytes)
            int pid = Process.GetCurrentProcess().Id;
            BitConverter.GetBytes(pid).CopyTo(buffer, 4); 

            // sequence number (2 bytes)
            BitConverter.GetBytes(++sequenceNumber).CopyTo(buffer, 6); 

            // Calculate and set Checksum
            ushort checksum = CalculateChecksum(buffer, buffer.Length);
            buffer[2] = (byte)(checksum >> 8);
            buffer[3] = (byte)(checksum & 0xFF);

            // might need to change the checksum if on mac
            // hit checksum with this: & 0xffff


            try
            {
                EndPoint remoteEndPoint = new IPEndPoint(ipAddress, 0);
                DateTime startTime = DateTime.Now;

                icmpSocket.SendTo(buffer, 8, SocketFlags.None, remoteEndPoint);
                icmpSocket.ReceiveFrom(buffer, ref remoteEndPoint);

                DateTime endTime = DateTime.Now;
                TimeSpan roundTripTime = endTime - startTime;

                //return $"Ping to {host} successful. Roundtrip time: {roundTripTime.TotalMilliseconds}ms";
                return roundTripTime.TotalMilliseconds;
            }
            catch (SocketException ex)
            {
                //return $"Ping to {host} failed: {ex.Message}";
                Debug.WriteLine(ex);
                return -3;
            }
            
        }

        private ushort CalculateChecksum(byte[] data, int length)
        {
            long sum = 0;

            for (int i = 0; i < length; i += 2)
            {
                if (i + 1 < length)
                {
                    sum += (ushort)((data[i] << 8) | data[i + 1]);
                }
                else
                {
                    sum += data[i];
                }
            }

            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }

            return (ushort)~sum;
        }
    }
}

