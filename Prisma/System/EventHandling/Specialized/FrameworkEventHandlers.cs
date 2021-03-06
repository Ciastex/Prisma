﻿using System.ComponentModel;
using Prisma.Natives.SDL;

namespace Prisma.System.EventHandling.Specialized
{
    internal class FrameworkEventHandlers
    {
        private EventDispatcher Dispatcher { get; }

        internal FrameworkEventHandlers(EventDispatcher dispatcher)
        {
            Dispatcher = dispatcher;

            Dispatcher.Discard(
                SDL2.SDL_EventType.SDL_CLIPBOARDUPDATE,
                SDL2.SDL_EventType.SDL_AUDIODEVICEADDED,
                SDL2.SDL_EventType.SDL_AUDIODEVICEREMOVED
            );
            
            Dispatcher.RegisterEventHandler(SDL2.SDL_EventType.SDL_QUIT, QuitRequested);
        }

        private void QuitRequested(Window owner, SDL2.SDL_Event ev)
            => owner.OnQuitRequested(new CancelEventArgs());
    }
}