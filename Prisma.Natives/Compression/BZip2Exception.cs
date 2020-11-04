using System;
using Chroma.Natives;

namespace Prisma.Natives.Compression
{
    internal class BZip2Exception : NativeExtractorException
    {
        public BZip2Exception(string message)
            : base(message)
        {
        }

        public BZip2Exception(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}