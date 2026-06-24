namespace Vault.Core
{
    /// <summary>
    /// A single filter condition applied to a property.
    /// </summary>
    public class QueryFilter
    {
        /// <summary>
        /// The name of the property to filter on.
        /// </summary>
        public string Property { get; set; } = "";

        /// <summary>
        /// The comparison operator to apply.
        /// </summary>
        public QueryOperator Operator { get; set; }

        /// <summary>
        /// Optional value used by operators that require one.
        /// </summary>
        public object? Value { get; set; }
    }

    /// <summary>
    /// Defines a single ORDER BY clause.
    /// </summary>
    public class QuerySort
    {
        /// <summary>
        /// The property to sort by.
        /// </summary>
        public string Property { get; set; } = "";

        /// <summary>
        /// Sort direction (ascending or descending).
        /// </summary>
        public QuerySortDirection Direction { get; set; } = QuerySortDirection.Asc;
    }

    /// <summary>
    /// Supported sort directions.
    /// </summary>
    public enum QuerySortDirection
    {
        Asc,
        Desc
    }

    /// <summary>
    /// A query consisting of multiple filters (AND logic)
    /// and optional ORDER BY clauses.
    /// </summary>
    public class QueryDefinition
    {
        /// <summary>
        /// A list of AND filters to apply.
        /// </summary>
        public List<QueryFilter> Filters { get; set; } = [];

        /// <summary>
        /// Optional ORDER BY clauses.
        /// If empty, no sorting is applied by the data handler.
        /// </summary>
        public List<QuerySort> Sorts { get; set; } = [];
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
        Empty,
        SelectEquals
    }
}
