using System;
using Chroma.Input;

namespace Prisma.Input
{
    public class ControllerButtonEventArgs : EventArgs
    {
        public ControllerInfo Controller { get; }
        public ControllerButton Button { get; }

        internal ControllerButtonEventArgs(ControllerInfo controller, ControllerButton button)
        {
            Controller = controller;
            Button = button;
        }
    }
}