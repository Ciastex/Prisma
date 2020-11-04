using System;
using System.Drawing;

namespace Prisma.System.EventHandling
{
    public class WindowSizeEventArgs : EventArgs
    {
        public Size Size { get; }

        internal WindowSizeEventArgs(Size size)
        {
            Size = size;
        }
    }
}