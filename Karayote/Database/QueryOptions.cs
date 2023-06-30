using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Karayote.Database
{
    public class QueryOptions<T>
    {
        public Expression<Func<T, object>>? OrderBy { get; set; }
        public Expression<Func<T, bool>>? Where { get; set; }


        public List<string> Includes { get; set; } = new List<string>();

        public bool HasWhere => Where != null;
        public bool HasOrderBy => OrderBy != null;
        public bool HasIncludes => Includes?.Count > 0;
    }
}
