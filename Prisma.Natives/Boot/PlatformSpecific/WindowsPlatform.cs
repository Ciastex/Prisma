﻿using System.Collections.Generic;
using System.IO;
using Chroma.Natives.Boot;

namespace Prisma.Natives.Boot.PlatformSpecific
{
    internal class WindowsPlatform : IPlatform
    {
        public NativeLibraryRegistry Registry { get; }

        public WindowsPlatform()
        {
            var paths = new List<string> { NativeLibraryExtractor.LibraryRoot };
            Registry = new NativeLibraryRegistry(paths);
        }

        public void Register(string libFilePath)
            => Registry.Register(Path.GetFileName(libFilePath));
    }
}
