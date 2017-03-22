using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace TcpSocketServer.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<Int64> ReadInt64Header(this Stream stream, CancellationToken? t = null)
        {
            var lengthHeaderBytes = new byte[sizeof(Int64)];

            var bytesRead = await stream.ReadIntoByteBuffer(lengthHeaderBytes, t);

            return bytesRead == 0 ? 0 : IPAddress.NetworkToHostOrder(
                 BitConverter.ToInt64(lengthHeaderBytes, 0));
        }

        public async static Task<int> ReadIntoByteBuffer(
            this Stream stream, byte[] buffer, CancellationToken? t = null)
        {
            var count = buffer.Length;
            var totalBytesRemaining = count;
            var totalBytesRead = 0;
            while (totalBytesRemaining != 0)
            {
                int bytesRead = 0;

                if (t.HasValue)
                {
                    bytesRead = await stream.ReadAsync(
                        buffer, totalBytesRead, totalBytesRemaining, t.Value);
                }
                else
                {
                    bytesRead = await stream.ReadAsync(
                        buffer, totalBytesRead, totalBytesRemaining);
                }

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
                totalBytesRemaining -= bytesRead;
            }

            return totalBytesRead;
        }
    }
}
