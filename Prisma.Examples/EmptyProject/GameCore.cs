using Prisma;
using Prisma.Diagnostics.Logging;
using Prisma.Input;

namespace EmptyProject
{
    public class GameCore : Game
    {
        private readonly Log _log = LogManager.GetForCurrentAssembly();
        
        public GameCore()
        {
            _log.Info("Hello, world!");
        }

        protected override void ControllerConnected(ControllerEventArgs e)
        {
            _log.Info($"Controller '{e.Controller.Name}' connected.");
        }
        
        protected override void ControllerAxisMoved(ControllerAxisEventArgs e)
        {
            _log.Info($"Controller axis '{e.Axis}' moved.");
        }

        protected override void ControllerButtonPressed(ControllerButtonEventArgs e)
        {
            _log.Info($"Controller button '{e.Button}' pressed.");
        }

        protected override void ControllerButtonReleased(ControllerButtonEventArgs e)
        {
            _log.Info($"Controller button '{e.Button}' released.");
        }

        protected override void ControllerDisconnected(ControllerEventArgs e)
        {
            _log.Info($"Controller '{e.Controller.Name}' disconnected.");
        }

        protected override void KeyPressed(KeyEventArgs e)
        {
            _log.Info($"Key '{e.KeyCode}' pressed.");
        }

        protected override void KeyReleased(KeyEventArgs e)
        {
            _log.Info($"Key '{e.KeyCode}' released.");
        }

        protected override void MouseMoved(MouseMoveEventArgs e)
        {
            Window.Title = e.Position.ToString();
        }

        protected override void MousePressed(MouseButtonEventArgs e)
        {
            _log.Info($"Mouse button '{e.Button}' pressed.");
        }

        protected override void MouseReleased(MouseButtonEventArgs e)
        {
            _log.Info($"Mouse button '{e.Button}' released.");
        }

        protected override void WheelMoved(MouseWheelEventArgs e)
        {
            _log.Info($"Mouse wheel moved: '{e.Motion.ToString()}'.");
        }
    }
}