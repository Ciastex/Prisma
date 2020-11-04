using System;

namespace Prisma.Natives
{
    internal class NativeExtractorException : Exception
    {
        public NativeExtractorException(string message)
            : base(message) { }

        public NativeExtractorException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}