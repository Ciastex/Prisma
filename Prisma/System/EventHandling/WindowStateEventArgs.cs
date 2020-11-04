using System;

namespace Prisma.System.EventHandling
{
    public class WindowStateEventArgs : EventArgs
    {
        public WindowState State { get; }

        internal WindowStateEventArgs(WindowState state)
        {
            State = state;
        }
    }
}