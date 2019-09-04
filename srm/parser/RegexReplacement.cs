//------------------------------------------------------------------------------
// <copyright file="RegexReplacement.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

// The RegexReplacement class represents a substitution string for
// use when using regexs to search/replace, etc. It's logically
// a sequence intermixed (1) constant strings and (2) group numbers.

namespace System.Text.RegularExpressions {

    using System.Collections;
    using System.Collections.Generic;

    internal sealed class RegexReplacement {
        /*
         * Since RegexReplacement shares the same parser as Regex,
         * the constructor takes a RegexNode which is a concatenation
         * of constant strings and backreferences.
         */
#if SILVERLIGHT
        internal RegexReplacement(String rep, RegexNode concat, Dictionary<Int32, Int32> _caps) {
#else
        internal RegexReplacement(String rep, RegexNode concat, Hashtable _caps) {
#endif
            StringBuilder sb;
            List<String> strings;
            List<Int32> rules;
            int slot;

            _rep = rep;

            if (concat.Type() != RegexNode.Concatenate)
                throw new ArgumentException(SR.GetString(SR.ReplacementError));

            sb = new StringBuilder();
            strings = new List<String>();
            rules = new List<Int32>();

            for (int i = 0; i < concat.ChildCount(); i++) {
                RegexNode child = concat.Child(i);

                switch (child.Type()) {
                    case RegexNode.Multi:
                        sb.Append(child._str);
                        break;
                    case RegexNode.One:
                        sb.Append(child._ch);
                        break;
                    case RegexNode.Ref:
                        if (sb.Length > 0) {
                            rules.Add(strings.Count);
                            strings.Add(sb.ToString());
                            sb.Length = 0;
                        }
                        slot = child._m;

                        if (_caps != null && slot >= 0)
                            slot = (int)_caps[slot];

                        rules.Add(-Specials - 1 - slot);
                        break;
                    default:
                        throw new ArgumentException(SR.GetString(SR.ReplacementError));
                }
            }

            if (sb.Length > 0) {
                rules.Add(strings.Count);
                strings.Add(sb.ToString());
            }

            _strings = strings; 
            _rules = rules; 
        }

        internal String _rep;
        internal List<String>  _strings;          // table of string constants
        internal List<Int32>  _rules;            // negative -> group #, positive -> string #

        // constants for special insertion patterns

        internal const int Specials       = 4;
        internal const int LeftPortion    = -1;
        internal const int RightPortion   = -2;
        internal const int LastGroup      = -3;
        internal const int WholeString    = -4;

        /*
         * The original pattern string
         */
        internal String Pattern {
            get {
                return _rep;
            }
        }
    }

}
