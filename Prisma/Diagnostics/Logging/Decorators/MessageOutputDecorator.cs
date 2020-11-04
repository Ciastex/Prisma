using Prisma.Diagnostics.Logging.Base;
using Prisma.Diagnostics.Logging.Sinks;
using Prisma.Extensions;

namespace Prisma.Diagnostics.Logging.Decorators
{
    public class MessageOutputDecorator : Decorator
    {
        public override string Decorate(LogLevel logLevel, string input, string originalMessage, Sink sink)
        {
            var output = originalMessage;

            if (sink is ConsoleSink)
                output = originalMessage.AnsiColorEncodeRGB(255, 255, 255);

            return output;
        }
    }
}