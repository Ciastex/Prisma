using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Prisma.Diagnostics.Logging;
using Prisma.Natives.SDL;
using Vulkan;

namespace Prisma.Graphics
{
    public class RenderContext
    {
        private Game _game;

        private Color _tint;

        private readonly Log _log = LogManager.GetForCurrentAssembly();
        private Queue<RenderOperationInfo> _operationQueue = new Queue<RenderOperationInfo>();

        public Color Tint
        {
            get => _tint;
            set
            {
                _tint = value;

                if (SDL2.SDL_SetRenderDrawColor(
                    _game.Graphics.SdlRendererHandle,
                    _tint.R,
                    _tint.G,
                    _tint.B,
                    _tint.A
                ) < 0)
                {
                    _log.Error($"Failed to set render tint color: {SDL2.SDL_GetError()}");
                }
            }
        }

        public Color ClearColor { get; set; } = Color.CornflowerBlue;

        internal RenderContext(Game game)
        {
            _game = game;
        }

        public void Rectangle(ShapeMode mode, Rectangle rect)
        {
            switch (mode)
            {
                case ShapeMode.Fill:
                    _operationQueue.Enqueue(
                        new RenderOperationInfo<Rectangle>(
                            RenderOperationType.FillRectangle,
                            rect
                        )
                    );
                    break;
            }
        }

        internal void FillCommandBuffer(CommandBuffer buffer)
        {
            if (_operationQueue.Any())
            {
                var op = _operationQueue.Dequeue();

                if (op.Type == RenderOperationType.FillRectangle)
                {
                    var frOp = op as RenderOperationInfo<Rectangle>;

                    buffer.CmdDraw(4, 1, 0, 0);
                }
            }
        }
    }
}