using System.ComponentModel.DataAnnotations;

namespace Template.Web.Domain.Entities.Base
{
    public class UniversalEntity : AggregateRoot
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
