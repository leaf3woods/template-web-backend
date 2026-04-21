namespace Template.Web.Domain.Entities.Base
{
    public interface ISoftDelete
    {
        public bool SoftDeleted { get; set; }

        public DateTime? DeleteTime { get; set; }
    }
}