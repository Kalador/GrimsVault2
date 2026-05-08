namespace Vault.Core
{
    /// <summary>
    /// A single filter condition.
    /// </summary>
    public class QueryFilter
    {
        public string Property { get; set; } = "";
        public QueryOperator Operator { get; set; }
        public object? Value { get; set; }
    }

    /// <summary>
    /// A query consisting of multiple filters (AND logic).
    /// </summary>
    public class QueryDefinition
    {
        public List<QueryFilter> Filters { get; set; } = [];
    }

    /// <summary>
    /// Supported comparison operators.
    /// </summary>
    public enum QueryOperator
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        GreaterThan,
        LessThan,
        NotEmpty,
        Empty
    }
}
