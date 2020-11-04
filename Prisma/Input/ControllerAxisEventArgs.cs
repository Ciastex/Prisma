using System;
using Chroma.Input;

namespace Prisma.Input
{
    public class ControllerAxisEventArgs : EventArgs
    {
        public ControllerInfo Controller { get; }
        public ControllerAxis Axis { get; }
        public short Value { get; }

        internal ControllerAxisEventArgs(ControllerInfo controller, ControllerAxis axis, short value)
        {
            Controller = controller;
            Axis = axis;
            Value = value;
        }
    }
}