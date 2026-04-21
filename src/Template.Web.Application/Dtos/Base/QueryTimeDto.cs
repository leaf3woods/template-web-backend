namespace Template.Web.Application.Dtos.Base
{
    public class QueryTimeDto : QueryDto
    {
        /// <summary>
        /// 查询开始时间条件
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 查询结束时间条件
        /// </summary>
        public DateTime? EndTime { get; set; }
    }
}