using System.Reflection;

namespace Prisma.Diagnostics.Logging
{
    internal class LogInfo
    {
        internal Assembly OwningAssembly { get; set; }
        internal Log Log { get; set; }
    }
}