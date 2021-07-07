using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Microsoft.SRM
{
    [Serializable]
    public class Regex
    {
        private static readonly CharSetSolver solver;
        static Regex()
        {
            solver = new CharSetSolver();
        }

        /// <summary>
        /// The unicode component includes the BDD algebra. It is being shared as a static member for efficiency.
        /// </summary>
        internal static readonly Unicode.UnicodeCategoryTheory<BDD> s_unicode = new Unicode.UnicodeCategoryTheory<BDD>(new CharSetSolver());

        internal const string _DFA_incompatible_with = "DFA option is incompatible with ";

        internal IMatcher _matcher;

        public Regex(string pattern) : this(pattern, RegexOptions.None) { }

        public Regex(string pattern, RegexOptions options) : this(pattern, options, System.Threading.Timeout.InfiniteTimeSpan) {}

        public Regex(string pattern, RegexOptions options, TimeSpan matchTimeout) : this(pattern, options, matchTimeout, null) {}

        public Regex(string pattern, RegexOptions options, TimeSpan matchTimeout, CultureInfo culture)
        {
            // Parse the input
            System.Text.RegularExpressions.RegexTree tree = System.Text.RegularExpressions.RegexParser.Parse(pattern, options,
                culture ?? ((options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture));

            // TBD: this could potentially be supported quite easily but is not of priority
            // it essentially affects how the iput string is being processed  -- characters are read backwards --
            // and what the right semantics of anchors is in this case (perhaps reversed)
            // if ((options & RegexOptions.RightToLeft) != 0)
            //     throw new NotSupportedException(SRM.Regex._DFA_incompatible_with + RegexOptions.RightToLeft);
            // TBD: this could also be supported easily, but is not of priority right now
            if ((options & RegexOptions.ECMAScript) != 0)
                throw new NotSupportedException(SRM.Regex._DFA_incompatible_with + RegexOptions.ECMAScript);
            // TBD: this will eventually be supported
            // if ((options & RegexOptions.Compiled) != 0)
            //     throw new NotSupportedException(SRM.Regex._DFA_incompatible_with + RegexOptions.Compiled);

            //fix the culture to be the given one unless it is null
            //in which case use the InvariantCulture if the option specifies CultureInvariant
            //otherwise use the current culture
            var theculture = culture ?? ((options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
            RegexToAutomatonConverter<BDD> converter = new RegexToAutomatonConverter<BDD>(s_unicode, theculture);
            CharSetSolver solver = (CharSetSolver)s_unicode.solver;
            var root = converter.ConvertNodeToSymbolicRegex(tree.Root, true);
            if (!root.info.ContainsSomeCharacter)
                throw new NotSupportedException(_DFA_incompatible_with + "characterless pattern");
            if (root.info.CanBeNullable)
                throw new NotSupportedException(_DFA_incompatible_with + "pattern allowing 0-length match");

            var partition = root.ComputeMinterms();
            if (partition.Length > 64)
            {
                //using BV to represent a predicate
                BVAlgebra algBV = new BVAlgebra(solver, partition);
                SymbolicRegexBuilder<BV> builderBV = new SymbolicRegexBuilder<BV>(algBV);
                //the default constructor sets the following predicates to False, this update happens after the fact
                //it depends on whether anchors where used in the regex whether the predicates are actually different from False
                builderBV.wordLetterPredicate = algBV.ConvertFromCharSet(solver, converter.srBuilder.wordLetterPredicate);
                builderBV.newLinePredicate = algBV.ConvertFromCharSet(solver, converter.srBuilder.newLinePredicate);
                //convert the BDD based AST to BV based AST
                SymbolicRegexNode<BV> rootBV = converter.srBuilder.Transform(root, builderBV, bdd => builderBV.solver.ConvertFromCharSet(solver, bdd));
                SymbolicRegexMatcher<BV> matcherBV = new SymbolicRegexMatcher<BV>(rootBV, solver, partition, options, matchTimeout, theculture);
                _matcher = matcherBV;
            }
            else
            {
                //using ulong to represent a predicate
                var alg64 = new BV64Algebra(solver, partition);
                var builder64 = new SymbolicRegexBuilder<ulong>(alg64);
                //the default constructor sets the following predicates to False, this update happens after the fact
                //it depends on whether anchors where used in the regex whether the predicates are actually different from False
                builder64.wordLetterPredicate = alg64.ConvertFromCharSet(solver, converter.srBuilder.wordLetterPredicate);
                builder64.newLinePredicate = alg64.ConvertFromCharSet(solver, converter.srBuilder.newLinePredicate);
                //convert the BDD based AST to ulong based AST
                SymbolicRegexNode<ulong> root64 = converter.srBuilder.Transform(root, builder64, bdd => builder64.solver.ConvertFromCharSet(solver, bdd));
                SymbolicRegexMatcher<ulong> matcher64 = new SymbolicRegexMatcher<ulong>(root64, solver, partition, options, matchTimeout, theculture);
                _matcher = matcher64;
            }
        }

        /// <summary>
        /// This constructor is invoked by the deserializer only.
        /// </summary>
        private Regex(IMatcher matcher) => _matcher = matcher;

        internal Match Run(bool quick, int prevlen, string input, int beg, int length, int startat)
        {
            if ((uint)startat > (uint)input.Length)
            {
                throw new ArgumentOutOfRangeException("startat");
            }
            if ((uint)length > (uint)input.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            int k = beg + length;

            // If the previous match was empty, advance by one before matching
            // or terminate the matching if there is no remaining input to search in
            if (prevlen == 0)
            {
                if (startat == k)
                    return Match.NoMatch;

                startat += 1;
            }

            var match = _matcher.FindMatch(quick, input, startat, k);
            if (quick)
            {
                if (match is null)
                    return null;
                else
                    return Match.NoMatch;
            }
            else if (match.Success)
            {
                return match;
            }
            else
                return Match.NoMatch;
        }

        /// <summary>
        /// Returns true iff the input string matches. 
        /// <param name="input">given iput string</param>
        /// <param name="startat">start position in the input</param>
        /// <param name="endat">end position in the input, -1 means that the value is unspecified and taken to be input.Length-1</param>
        /// </summary>
        public bool IsMatch(string input, int startat = 0, int endat = -1) {
            int k = endat + 1;
            if (k == 0) {
                k = input.Length;
            }
            return _matcher.FindMatch(true, input, startat, k) is null;
        }

        /// <summary>
        /// Returns all matches as pairs (startindex, length) in the input string.
        /// </summary>
        /// <param name="input">given iput string</param>
        /// <param name="limit">as soon as this many matches have been found the search terminates, 0 or negative value means that there is no bound, default is 0</param>
        /// <param name="startat">start position in the input, default is 0</param>
        /// <param name="endat">end position in the input, -1 means that the value is unspecified and taken to be input.Length-1</param>
        public List<Match> Matches(string input, int limit = 0, int startat = 0, int endat = -1) {
            int k = endat + 1;
            if (k == 0) {
                k = input.Length;
            }
            List<Match> results = new List<Match>();
            Match result = _matcher.FindMatch(false, input, startat, k);
            while (result.Success) {
                results.Add(result);
                int newStart = result.Index + Math.Max(1, result.Length);
                if (newStart >= input.Length)
                    break;
                result = _matcher.FindMatch(false, input, newStart, k);
            }
            return results;
        }

        /// <summary>
        /// Serialize the matcher by appending it into sb.
        /// Uses characters only from visible ASCII range.
        /// </summary>
        private void SerializeStringBuilder(StringBuilder sb) => _matcher.Serialize(sb);

        //it must not be '\n' or a character used to serialize the fragments: 0-9A-Za-z/\+*()[].,-^$;?
        //avoiding '\n' so that multiple serializations can be stored one per line in an ascii text file
        internal const char s_top_level_separator = '#';

        /// <summary>
        /// Deserializes the matcher from the given input string created with Serialize.
        /// </summary>
        private static Regex DeserializeString(string input)
        {
            input.Split(s_top_level_separator);
            string[] fragments = input.Split(s_top_level_separator);
            if (fragments.Length != 15)
                throw new ArgumentException($"{nameof(Regex.Deserialize)} error", nameof(input));

            try
            {
                BVAlgebraBase alg = BVAlgebraBase.Deserialize(fragments[1]);
                IMatcher matcher = alg is BV64Algebra ?
                    (IMatcher)new SymbolicRegexMatcher<ulong>(alg as BV64Algebra, fragments) :
                    (IMatcher)new SymbolicRegexMatcher<BV>(alg as BVAlgebra, fragments);
                return new Regex(matcher);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"{nameof(Regex.Deserialize)} error", nameof(input), e);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            SerializeStringBuilder(sb);
            return sb.ToString();
        }

        public void SaveDGML(TextWriter writer, int bound, bool hideStateInfo, bool addDotStar, bool inReverse, bool onlyDFAinfo, int maxLabelLength)
        {
            _matcher.SaveDGML(writer, bound, hideStateInfo, addDotStar, inReverse, onlyDFAinfo, maxLabelLength);
        }
        
        /// <summary>
        /// Serialize this symbolic regex matcher to the given file.
        /// If formatter is null then an instance of 
        /// System.Runtime.Serialization.Formatters.Binary.BinaryFormatter is used.
        /// </summary>
        /// <param name="file">file where the serialization is stored</param>
        /// <param name="formatter">given formatter</param>
        public void Serialize(string file)
        {
            Serialize(new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None));
        }

        /// <summary>
        /// Serialize this symbolic regex matcher to the given file.
        /// If formatter is null then an instance of 
        /// System.Runtime.Serialization.Formatters.Binary.BinaryFormatter is used.
        /// </summary>
        /// <param name="stream">stream where the serialization is stored</param>
        /// <param name="formatter">given formatter</param>
        public void Serialize(Stream stream)
        {
            var streamWriter = new StreamWriter(stream);
            StringBuilder sb = new StringBuilder();
            SerializeStringBuilder(sb);
            streamWriter.Write(sb.ToString());
            streamWriter.Close();
        }

        /// <summary>
        /// Deserialize the matcher of a symblic regex from the given file using the given formatter. 
        /// If formatter is null then an instance of 
        /// System.Runtime.Serialization.Formatters.Binary.BinaryFormatter is used.
        /// </summary>
        /// <param name="file">source file of the serialized matcher</param>
        /// <param name="formatter">given formatter</param>
        /// <returns></returns>
        public static Regex Deserialize(string file)
        {
            return Deserialize(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        /// <summary>
        /// Deserialize the matcher of a symblic regex from the given stream using the given formatter. 
        /// If formatter is null then an instance of 
        /// System.Runtime.Serialization.Formatters.Binary.BinaryFormatter is used.
        /// </summary>
        /// <param name="stream">source stream of the serialized matcher</param>
        /// <param name="formatter">given formatter</param>
        /// <returns></returns>
        public static Regex Deserialize(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            Regex matcher = DeserializeString(streamReader.ReadToEnd());
            streamReader.Close();
            return matcher;
        }
    }
}
