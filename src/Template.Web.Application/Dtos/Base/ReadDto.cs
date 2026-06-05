namespace Template.Web.Application.Dtos.Base
{
    public abstract class ReadDto : IReadDto
    {
        public Guid Id { get; set; }
    }

    public abstract class ReadDto<TKey> : IReadDto
        where TKey : struct
    {
        public TKey Id { get; set; } = default!;
    }
}
