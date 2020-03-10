
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SRM
{
    [TestClass]
    public class RegexMatcherTests
    {
        [TestMethod]
        public void TestSRM()
        {
            var sr = new Microsoft.SRM.Regex(@"a[^ab]+b");
            var input = "xaTAG1bxaTAG2bc";
            var matches = sr.Matches(input);
            Assert.IsTrue(matches.Count == 2);
            Assert.IsTrue(matches[0].Index == 1);
            Assert.IsTrue(matches[0].Length == 6);
            Assert.IsTrue(matches[1].Index == 8);
            Assert.IsTrue(matches[1].Length == 6);
            sr.Serialize("tag.bin");
            var sr2 = Microsoft.SRM.Regex.Deserialize("tag.bin");
            var matches2 = sr2.Matches(input);
            CollectionAssert.AreEqual(matches, matches2);
        }

        [TestMethod]
        public void TestSRM_singlePass()
        {
            var sr = new Microsoft.SRM.Regex(@"abcbc1|cbc2");
            var input = "xxxabcbc1yyyccbc2xxx";
            var matches = sr.Matches(input);
            Assert.IsTrue(matches.Count == 2);
            Assert.IsTrue(matches[0].Index == 3);
            Assert.IsTrue(matches[0].Length == 6);
            Assert.IsTrue(matches[1].Index == 13);
            Assert.IsTrue(matches[1].Length == 4);
            sr.Serialize("tag.bin");
            var sr2 = Microsoft.SRM.Regex.Deserialize("tag.bin");
            var matches2 = sr2.Matches(input);
            CollectionAssert.AreEqual(matches, matches2);
        }

        [TestMethod]
        public void TestSRM_singletonSeq()
        {
            var sr = new Microsoft.SRM.Regex(@"a[bB]c");
            var input = "xxxabcyyyaBcxxx";
            var matches = sr.Matches(input);
            Assert.IsTrue(matches.Count == 2);
            Assert.IsTrue(matches[0].Index == 3);
            Assert.IsTrue(matches[0].Length == 3);
            Assert.IsTrue(matches[1].Index == 9);
            Assert.IsTrue(matches[1].Length == 3);
            sr.Serialize("tag.bin");
            var sr2 = Microsoft.SRM.Regex.Deserialize("tag.bin");
            var matches2 = sr2.Matches(input);
            CollectionAssert.AreEqual(matches, matches2);
        }
    }
}