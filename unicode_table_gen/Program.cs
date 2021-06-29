using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SRM
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("usage: unicode_table_gen <target directory>");
                return 1;
            }
            string targetDirectory = args[0];
            Unicode.UnicodeCategoryRangesGenerator.Generate("Microsoft.SRM.Generated", "UnicodeCategoryRanges", targetDirectory);
            Unicode.IgnoreCaseRelationGenerator.Generate("Microsoft.SRM.Generated", "IgnoreCaseRelation", targetDirectory);
            return 0;
        }
    }
}
