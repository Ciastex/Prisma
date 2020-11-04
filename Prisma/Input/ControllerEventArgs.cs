using System;
using Chroma.Input;

namespace Prisma.Input
{
    public class ControllerEventArgs : EventArgs
    {
        public ControllerInfo Controller { get; }

        internal ControllerEventArgs(ControllerInfo controller)
        {
            Controller = controller;
        }
    }
}