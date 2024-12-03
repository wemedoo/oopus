namespace sReportsV2.Domain.Sql.Entities.Common
{
    public class QueryEntityParam<T>
    {
        public int Id { get; set; }

        public QueryEntityParam(int id)
        {
            this.Id = id;
        }
    }
}
