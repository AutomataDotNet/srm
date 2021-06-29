// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// The RegexReplacement class represents a substitution string for
    /// use when using regexes to search/replace, etc. It's logically
    /// a sequence intermixed (1) constant strings and (2) group numbers.
    /// </summary>
    internal sealed class RegexReplacement
    {
        // Constants for special insertion patterns
        private const int Specials = 4;
        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;

        private readonly string[] _strings; // table of string constants
        private readonly int[] _rules;      // negative -> group #, positive -> string #
    }
}
