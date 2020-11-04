using System;

namespace Prisma.Natives
{
    internal class NativeLibraryException : Exception
    {
        public NativeLibraryException(string message) : base(message) { }
    }
}
