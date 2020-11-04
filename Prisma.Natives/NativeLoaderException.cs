using System;

namespace Prisma.Natives
{
    internal class NativeLoaderException : Exception
    {
        public NativeLoaderException(string message) : base(message) { }
    }
}
