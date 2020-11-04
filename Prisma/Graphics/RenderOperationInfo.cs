namespace Prisma.Graphics
{
    public class RenderOperationInfo
    {
        public RenderOperationType Type { get; }

        public RenderOperationInfo(RenderOperationType type)
        {
            Type = type;
        }
    }

    public class RenderOperationInfo<T> : RenderOperationInfo
    {
        public T Data { get; set; }

        public RenderOperationInfo(RenderOperationType type, T data) : base(type)
        {
            Data = data;
        }
    }
}