using System;
using System.Numerics;
using Prisma.Natives.SDL;

namespace Prisma.Input
{
    public class MouseWheelEventArgs : EventArgs
    {
        public Vector2 Motion { get; }
        public bool DirectionFlipped { get; }

        internal MouseWheelEventArgs(Vector2 motion, uint direction)
        {
            Motion = motion;
            DirectionFlipped = direction == (uint)SDL2.SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED;
        }
    }
}