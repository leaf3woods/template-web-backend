namespace Template.Web.Domain.Entities.Base.Audited
{
    public interface ILastModificationAudited
    {
        Guid? LastModifierId { get; set; }
        DateTime? LastModificationTime { get; set; }
    }
}