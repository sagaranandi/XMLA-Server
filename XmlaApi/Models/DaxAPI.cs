namespace XmlaApi.Models
{
    public class DaxAPI
    {
        public string WorkspaceName { get; set; }
        public string DatasetName { get; set; }
        public string? DaxQuery { get; set; }

        public List<GroupBy> GroupBy { get; set; }
        public List<Aggregate> Aggregate { get; set; }
    }

    public class GroupBy
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Aggregate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AggregationType { get; set; }
    }
}
