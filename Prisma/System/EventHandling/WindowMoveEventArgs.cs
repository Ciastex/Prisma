using System;
using System.Numerics;

namespace Prisma.System.EventHandling
{
    public class WindowMoveEventArgs : EventArgs
    {
        public Vector2 Position { get; }

        internal WindowMoveEventArgs(Vector2 position)
        {
            Position = position;
        }
    }
}