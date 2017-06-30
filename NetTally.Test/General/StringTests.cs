using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass]
    public class StringTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            using (new RegionProfiler("warmup"))
            {
                Assert.AreEqual("resume", "resume".RemoveDiacritics());
            }
        }

        [TestMethod]
        public void RemoveDiacriticals_baseline()
        {
            using (new RegionProfiler("baseline"))
            {
                Assert.AreEqual("resume", "resume".RemoveDiacritics());
            }
        }

        [TestMethod]
        public void RemoveDiacriticals_examples()
        {
            using (new RegionProfiler("examples (17)"))
            {
                Assert.AreEqual("resume", "résumé".RemoveDiacritics());
                Assert.AreEqual("Resume", "Résumé".RemoveDiacritics());
                Assert.AreEqual("Comme ci, comme ca", "Comme ci, comme ça".RemoveDiacritics());
                Assert.AreEqual("habia", "había".RemoveDiacritics());
                Assert.AreEqual("naive", "naïve".RemoveDiacritics());
                Assert.AreEqual("NAIVE", "NAİVE".RemoveDiacritics());
                Assert.AreEqual("hrabe", "hrábě".RemoveDiacritics());
                Assert.AreEqual("normal", "normal".RemoveDiacritics());
                Assert.AreEqual("Elaeudanla Teiteia", "Elaeudanla Téïtéïa".RemoveDiacritics());
                Assert.AreEqual("Laetitia", "Lætitia".RemoveDiacritics());
                Assert.AreEqual("AEsir", "Æsir".RemoveDiacritics());
                Assert.AreEqual("AEro", "Ærø".RemoveDiacritics());
                Assert.AreEqual("aetate", "ætate".RemoveDiacritics());
                Assert.AreEqual("Wasserschloss", "Waſſerschloſʒ".RemoveDiacritics());
                Assert.AreEqual("Wasserschloss", "Wasserschloß".RemoveDiacritics());
                Assert.AreEqual("beissen", "beißen".RemoveDiacritics());
                Assert.AreEqual("gruessen", "grüßen".RemoveDiacritics());
            }
        }

        [TestMethod]
        public void RemoveDiacriticals_long_sentence()
        {
            using (new RegionProfiler("long sentence"))
            {
                Assert.AreEqual("While the snow and wind were blocked by the ward around the area, the blizzard still raging outside of the transparent barrier, that just made things more tolerable rather than warm. The cold never bothered me anyway, at least at this level, but Bao-Bao looked as though warming up before getting into a dangerous situation might be for the best.",
                    "While the snow and wind were blocked by the ward around the area, the blizzard still raging outside of the transparent barrier, that just made things more tolerable rather than warm. The cold never bothered me anyway, at least at this level, but Bao-Bao looked as though warming up before getting into a dangerous situation might be for the best.".RemoveDiacritics());
            }
        }

        [TestMethod]
        public void RemoveDiacriticals_valid()
        {
            using (new RegionProfiler("valid (5)"))
            {
                Assert.AreEqual("To do — finally.", "To do — finally.".RemoveDiacritics());
                Assert.AreEqual("Today! 😄", "Today! 😄".RemoveDiacritics());
                Assert.AreEqual("Critters [🐞🦊🐰🐿]", "Critters [🐞🦊🐰🐿]".RemoveDiacritics());
                Assert.AreEqual("◈Attack the Tree", "◈Attack the Tree".RemoveDiacritics());
                Assert.AreEqual("だからねー、皆さん思うでしょ。", "だからねー、皆さん思うでしょ。".RemoveDiacritics());
            }
        }
    }
}
