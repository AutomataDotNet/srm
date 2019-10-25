using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SRM
{
    class Program
    {
        static void Main(string[] args)
        {
            UnicodeCategoryRangesGenerator.Generate("Microsoft.SRM.Generated", "UnicodeCategoryRanges", "");
            IgnoreCaseRelationGenerator.Generate("Microsoft.SRM.Generated", "IgnoreCaseRelation", "");
        }
    }
}
