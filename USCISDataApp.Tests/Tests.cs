using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ClassLibrary;


namespace USCISDataApp.Tests
{
    [TestFixture]
    public class Tests
    {
        private Converter converter;
        [SetUp]
        public void Setup()
        {
            converter = new Converter();
        }

        [Test]
        public void SplitUscisIdTest()
        {

            var result = converter.SplitUscisNum("Wac2190093185");
            
            Dictionary<string, uint> expected = new Dictionary<string, uint>();
            expected.Add("WAC", 2190033825);
            Assert.IsTrue(expected.All(e => result.Contains(e)));
        }

        [Test]
        public void GenerateCaseListTest()
        {
            Dictionary<string, uint> testCaseId = new Dictionary<string, uint>();
            testCaseId.Add("WAC", 190033825);
            var result = converter.GenerateCaseList(testCaseId, 2, 2);
            List<string> expected = new List<string>()
            {
                "WAC0190033823",
                "WAC0190033824",
                "WAC0190033825",
                "WAC0190033826",
                "WAC0190033827"
            };

            CollectionAssert.AreEqual(expected,result);
        }
    }
}