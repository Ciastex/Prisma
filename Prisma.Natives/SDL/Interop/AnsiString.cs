﻿using System;
using System.Runtime.InteropServices;

namespace Prisma.Natives.SDL.Interop
{
    internal struct AnsiString
    {
        public IntPtr Pointer;

        public string Value => Marshal.PtrToStringAnsi(Pointer);

        public override string ToString()
            => Value;
    }
}
