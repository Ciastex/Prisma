using System;
using System.Text;

namespace Prisma.System
{
    public class EngineException : Exception
    {
        public string SdlMessage { get; }

        public EngineException(string message, string sdlMessage) : base(message)
        {
            SdlMessage = sdlMessage;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Message);
            sb.Append(' ');
            sb.Append(SdlMessage);

            return sb.ToString();
        }
    }
}