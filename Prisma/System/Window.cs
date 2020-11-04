using System;
using System.Drawing;
using System.Numerics;
using Prisma.Diagnostics.Logging;
using Prisma.Graphics;
using Prisma.MemoryManagement;
using Prisma.Natives.SDL;
using Prisma.System.EventHandling;
using Prisma.System.EventHandling.Specialized;

namespace Prisma.System
{
    internal delegate void UpdateDelegate(double delta);
    internal delegate void DrawDelegate(RenderContext context);

    public class Window : DisposableResource
    {
        private readonly Log _log = LogManager.GetForCurrentAssembly();

        private Size _size;
        private Size _minSize;
        private Size _maxSize;

        private Vector2 _position = Vector2.Zero;
        private WindowState _state = WindowState.Normal;
        private string _title = string.Empty;

        private double _delta;
        private UpdateDelegate _updateDelegate;
        private DrawDelegate _drawDelegate;

        internal Game Game { get; private set; }
        internal IntPtr SdlWindowHandle { get; private set; }
        internal EventDispatcher EventDispatcher { get; private set; }

        public event EventHandler Closed;
        public event EventHandler Hidden;
        public event EventHandler Shown;
        public event EventHandler Invalidated;
        public event EventHandler<WindowStateEventArgs> StateChanged;
        public event EventHandler MouseEntered;
        public event EventHandler MouseLeft;
        public event EventHandler Focused;
        public event EventHandler Unfocused;
        public event EventHandler<WindowMoveEventArgs> Moved;
        public event EventHandler<WindowSizeEventArgs> SizeChanged;
        public event EventHandler<WindowSizeEventArgs> Resized;
        public event EventHandler<CancelEventArgs> QuitRequested;

        public bool Exists { get; private set; }

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;

                SDL2.SDL_SetWindowPosition(
                    SdlWindowHandle,
                    (int)_position.X,
                    (int)_position.Y
                );
            }
        }

        public Size Size
        {
            get => _size;
            set
            {
                _size = value;

                SDL2.SDL_SetWindowSize(
                    SdlWindowHandle,
                    _size.Width,
                    _size.Height
                );
            }
        }

        public Size MinSize
        {
            get => _minSize;
            set
            {
                _minSize = value;

                SDL2.SDL_SetWindowMinimumSize(
                    SdlWindowHandle,
                    _minSize.Width,
                    _minSize.Height
                );
            }
        }

        public Size MaxSize
        {
            get => _maxSize;
            set
            {
                _maxSize = value;

                SDL2.SDL_SetWindowMaximumSize(
                    SdlWindowHandle,
                    _maxSize.Width,
                    _maxSize.Height
                );
            }
        }

        public WindowState State
        {
            get => _state;

            set
            {
                _state = value;

                if (SdlWindowHandle != IntPtr.Zero)
                {
                    switch (value)
                    {
                        case WindowState.Maximized:
                            var flags = (SDL2.SDL_WindowFlags)SDL2.SDL_GetWindowFlags(SdlWindowHandle);

                            if (!flags.HasFlag(SDL2.SDL_WindowFlags.SDL_WINDOW_RESIZABLE))
                            {
                                _log.Warning("Refusing to maximize a non-resizable window.");
                                return;
                            }

                            SDL2.SDL_MaximizeWindow(SdlWindowHandle);
                            break;

                        case WindowState.Minimized:
                            SDL2.SDL_MinimizeWindow(SdlWindowHandle);
                            break;

                        case WindowState.Normal:
                            SDL2.SDL_RestoreWindow(SdlWindowHandle);
                            break;
                    }
                }
            }
        }

        public bool CanResize
        {
            get
            {
                var flags = (SDL2.SDL_WindowFlags)SDL2.SDL_GetWindowFlags(SdlWindowHandle);
                return flags.HasFlag(SDL2.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            }

            set => SDL2.SDL_SetWindowResizable(
                SdlWindowHandle,
                value
                    ? SDL2.SDL_bool.SDL_TRUE
                    : SDL2.SDL_bool.SDL_FALSE
            );
        }

        public bool BorderEnabled
        {
            get
            {
                var flags = (SDL2.SDL_WindowFlags)SDL2.SDL_GetWindowFlags(SdlWindowHandle);
                return !flags.HasFlag(SDL2.SDL_WindowFlags.SDL_WINDOW_BORDERLESS);
            }

            set => SDL2.SDL_SetWindowBordered(
                SdlWindowHandle,
                value
                    ? SDL2.SDL_bool.SDL_TRUE
                    : SDL2.SDL_bool.SDL_FALSE
            );
        }

        public bool IsFullScreen
        {
            get
            {
                var flags = (SDL2.SDL_WindowFlags)SDL2.SDL_GetWindowFlags(SdlWindowHandle);

                return flags.HasFlag(SDL2.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN)
                       || flags.HasFlag(SDL2.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            }
        }

        public bool IsCursorGrabbed
        {
            get => SDL2.SDL_GetWindowGrab(SdlWindowHandle) == SDL2.SDL_bool.SDL_TRUE;
            set => SDL2.SDL_SetWindowGrab(
                SdlWindowHandle,
                value
                    ? SDL2.SDL_bool.SDL_TRUE
                    : SDL2.SDL_bool.SDL_FALSE
            );
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;

                SDL2.SDL_SetWindowTitle(
                    SdlWindowHandle,
                    _title
                );
            }
        }

        internal Window(Game game, Size size, UpdateDelegate updateDelegate, DrawDelegate drawDelegate)
        {
            Game = game;

            _size = size;
            _updateDelegate = updateDelegate;
            _drawDelegate = drawDelegate;
        }

        internal void Run()
        {
            Exists = true;

            while (Exists)
            {
                while (SDL2.SDL_PollEvent(out var ev) != 0)
                    EventDispatcher.Dispatch(ev);

                _updateDelegate(_delta);

                Game.Graphics.DrawFrame(_drawDelegate);
            }
        }

        internal GraphicsManager Initialize()
        {
            if (SDL2.SDL_CreateWindowAndRenderer(
                _size.Width,
                _size.Height,
                SDL2.SDL_WindowFlags.SDL_WINDOW_SHOWN
                | SDL2.SDL_WindowFlags.SDL_WINDOW_VULKAN
                | SDL2.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI,
                out var sdlWindowHandle,
                out var sdlRendererHandle
            ) < 0)
            {
                throw new EngineException($"Failed to initialize the engine.", SDL2.SDL_GetError());
            }

            SdlWindowHandle = sdlWindowHandle;

            Title = "Prisma Engine";
            Position = new Vector2(SDL2.SDL_WINDOWPOS_CENTERED, SDL2.SDL_WINDOWPOS_CENTERED);
            State = WindowState.Normal;

            var mgr = new GraphicsManager(Game, sdlRendererHandle);

            EventDispatcher = new EventDispatcher(this);
            _ = new WindowEventHandlers(EventDispatcher);
            _ = new FrameworkEventHandlers(EventDispatcher);
            _ = new InputEventHandlers(EventDispatcher);

            return mgr;
        }

        internal void OnClosed()
            => Closed?.Invoke(this, EventArgs.Empty);

        internal void OnHidden()
            => Hidden?.Invoke(this, EventArgs.Empty);

        internal void OnShown()
            => Shown?.Invoke(this, EventArgs.Empty);

        internal void OnInvalidated()
        {
            SDL2.SDL_RenderPresent(Game.Graphics.SdlRendererHandle);
            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        internal void OnStateChanged(WindowStateEventArgs e)
        {
            _state = e.State;
            StateChanged?.Invoke(this, e);
        }

        internal void OnMouseEntered()
            => MouseEntered?.Invoke(this, EventArgs.Empty);

        internal void OnMouseLeft()
            => MouseLeft?.Invoke(this, EventArgs.Empty);

        internal void OnFocusOffered()
        {
            SDL2.SDL_SetWindowInputFocus(SdlWindowHandle);
        }

        internal void OnFocused()
            => Focused?.Invoke(this, EventArgs.Empty);

        internal void OnUnfocused()
            => Unfocused?.Invoke(this, EventArgs.Empty);

        internal void OnMoved(WindowMoveEventArgs e)
        {
            _position = e.Position;

            Moved?.Invoke(this, e);
        }

        internal void OnSizeChanged(WindowSizeEventArgs e)
        {
            _size = e.Size;

            SizeChanged?.Invoke(this, e);
        }

        internal void OnResized(WindowSizeEventArgs e)
        {
            Resized?.Invoke(this, e);
        }

        internal void OnQuitRequested(CancelEventArgs e)
        {
            QuitRequested?.Invoke(this, e);

            if (!e.Cancel)
                Exists = false;
        }

        protected override void FreeNativeResources()
        {
            SDL2.SDL_Quit();
        }
    }
}