using System;

namespace Prisma.System.EventHandling
{
    public class CancelEventArgs : EventArgs
    {
        public bool Cancel { get; set; }

        internal CancelEventArgs()
        {
        }
    }
}