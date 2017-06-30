using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass]
    public class UtilityTests
    {
        [TestMethod]
        public void RemoveDiacriticals()
        {
            using (new RegionProfiler("diacriticals1"))
            {
                Assert.AreEqual("resume", "resume".RemoveDiacritics());
            }

            using (new RegionProfiler("diacriticals2 (17)"))
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

            using (new RegionProfiler("diacriticals3"))
            {
                Assert.AreEqual("While the snow and wind were blocked by the ward around the area, the blizzard still raging outside of the transparent barrier, that just made things more tolerable rather than warm. The cold never bothered me anyway, at least at this level, but Bao-Bao looked as though warming up before getting into a dangerous situation might be for the best.",
                    "While the snow and wind were blocked by the ward around the area, the blizzard still raging outside of the transparent barrier, that just made things more tolerable rather than warm. The cold never bothered me anyway, at least at this level, but Bao-Bao looked as though warming up before getting into a dangerous situation might be for the best.".RemoveDiacritics());
            }
        }
    }
}
