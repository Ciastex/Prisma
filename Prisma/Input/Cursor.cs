// using System;
// using System.Numerics;
// using Prisma.Diagnostics.Logging;
// using Prisma.MemoryManagement;
// using Prisma.Natives.SDL;
//
// namespace Prisma.Input
// {
//     public class Cursor : DisposableResource
//     {
//         private static bool _isVisible = true;
//         private IntPtr _cursorHandle;
//
//         private Log Log { get; } = LogManager.GetForCurrentAssembly();
//
//         public static bool IsVisible
//         {
//             get => _isVisible;
//             set
//             {
//                 _isVisible = value;
//
//                 if (_isVisible)
//                     SDL2.SDL_ShowCursor(1);
//                 else SDL2.SDL_ShowCursor(0);
//             }
//         }
//
//         public Texture Texture { get; }
//         public Vector2 HotSpot { get; }
//
//         public bool IsCurrent => SDL2.SDL_GetCursor() == _cursorHandle;
//
//         public Cursor(string imageFilePath, Vector2 hotSpot)
//         {
//             Texture = new Texture(imageFilePath);
//             HotSpot = hotSpot;
//
//             Initialize();
//         }
//
//         public Cursor(Texture texture, Vector2 hotSpot)
//         {
//             if (texture.Disposed)
//                 throw new InvalidOperationException("The texture you provided was previously disposed.");
//
//             Texture = new Texture(texture);
//             HotSpot = hotSpot;
//
//             Initialize();
//         }
//
//         public void SetCurrent()
//         {
//             EnsureNotDisposed();
//             SDL2.SDL_SetCursor(_cursorHandle);
//         }
//
//         public static void Reset()
//             => SDL2.SDL_SetCursor(SDL2.SDL_GetDefaultCursor());
//
//         protected override void FreeNativeResources()
//         {
//             if (_cursorHandle != IntPtr.Zero)
//                 SDL2.SDL_FreeCursor(_cursorHandle);
//         }
//
//         protected override void FreeManagedResources()
//             => Texture.Dispose();
//
//         private void Initialize()
//         {
//             unsafe
//             {
//                 _cursorHandle = SDL2.SDL_CreateColorCursor(
//                     new IntPtr(Texture.Surface),
//                     (int)HotSpot.X,
//                     (int)HotSpot.Y
//                 );
//
//                 if (_cursorHandle == IntPtr.Zero)
//                     Log.Error($"Failed to load the cursor: {SDL2.SDL_GetError()}");
//             }
//         }
//     }
// }