using System;
using System.Globalization;
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

        #region Diacriticals
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
        #endregion

        #region Plan names
        [TestMethod]
        public void PlanName_make()
        {
            string name = "Kinematics";
            string planName = name.MakePlanName();

            Assert.AreEqual("◈Kinematics", planName);
        }

        [TestMethod]
        public void PlanName_make_empty()
        {
            string name = "";
            string planName = name.MakePlanName();

            Assert.AreEqual("", planName);
        }

        [TestMethod]
        public void PlanName_make_null()
        {
            string planName = Strings.MakePlanName(null);

            Assert.AreEqual("", planName);
        }

        [TestMethod]
        public void PlanName_make_redundant()
        {
            string name = "◈Kinematics";
            string planName = name.MakePlanName();

            Assert.AreEqual("◈Kinematics", planName);
        }

        [TestMethod]
        public void PlanName_test()
        {
            string name = "◈Kinematics";
            Assert.IsTrue(name.IsPlanName());
        }

        [TestMethod]
        public void PlanName_test_false()
        {
            string name = "Kinematics";
            Assert.IsFalse(name.IsPlanName());
        }

        [TestMethod]
        public void PlanName_test_empty()
        {
            string name = "";
            Assert.IsFalse(name.IsPlanName());
        }

        [TestMethod]
        public void PlanName_test_untrimmed()
        {
            string name = "  ◈Kinematics  ";
            Assert.IsFalse(name.IsPlanName());
        }

        [TestMethod]
        public void PlanName_test_embedded()
        {
            string name = "Kinema◈tics";
            Assert.IsFalse(name.IsPlanName());
        }

        [TestMethod]
        public void PlanName_test_null()
        {
            Assert.IsFalse(Strings.IsPlanName(null));
        }
        #endregion

        #region Line splitting
        [TestMethod]
        public void Split_normal_1()
        {
            string input =
@"Saying one thing
and then another";

            var lines = input.GetStringLines();

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("Saying one thing", lines[0]);
            Assert.AreEqual("and then another", lines[1]);
        }

        [TestMethod]
        public void Split_normal_2()
        {
            string input =
@"
Saying one thing
and then another";

            var lines = input.GetStringLines();

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("Saying one thing", lines[0]);
            Assert.AreEqual("and then another", lines[1]);
        }

        [TestMethod]
        public void Split_normal_3()
        {
            string input =
@"Saying one thing

and then another";

            var lines = input.GetStringLines();

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("Saying one thing", lines[0]);
            Assert.AreEqual("and then another", lines[1]);
        }

        public void Split_simple()
        {
            string input =
@"Saying one thing";

            var lines = input.GetStringLines();

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("Saying one thing", lines[0]);
        }

        [TestMethod]
        public void Split_null_1()
        {
            string input = null;
            var lines = input.GetStringLines();

            Assert.IsNotNull(lines);
            Assert.AreEqual(0, lines.Count);
        }

        [TestMethod]
        public void Split_null_2()
        {
            var lines = Strings.GetStringLines(null);

            Assert.IsNotNull(lines);
            Assert.AreEqual(0, lines.Count);
        }

        [TestMethod]
        public void Split_empty()
        {
            string input = "";
            var lines = input.GetStringLines();

            Assert.IsNotNull(lines);
            Assert.AreEqual(0, lines.Count);
        }

        [TestMethod]
        public void FirstLine_normal_1()
        {
            string input =
@"Saying one thing
and then another";

            var line = input.GetFirstLine();

            Assert.IsNotNull(line);
            Assert.AreEqual("Saying one thing", line);
        }

        [TestMethod]
        public void FirstLine_normal_2()
        {
            string input =
@"
Saying one thing
and then another";

            var line = input.GetFirstLine();

            Assert.IsNotNull(line);
            Assert.AreEqual("Saying one thing", line);
        }

        [TestMethod]
        public void FirstLine_simple()
        {
            string input =
@"Saying one thing";

            var line = input.GetFirstLine();

            Assert.IsNotNull(line);
            Assert.AreEqual("Saying one thing", line);
        }

        [TestMethod]
        public void FirstLine_null()
        {
            string input = null;

            var line = input.GetFirstLine();

            Assert.IsNotNull(line);
            Assert.AreEqual("", line);
        }

        [TestMethod]
        public void FirstLine_empty()
        {
            string input = "";

            var line = input.GetFirstLine();

            Assert.IsNotNull(line);
            Assert.AreEqual("", line);
        }
        #endregion

        #region Safe strings
        [TestMethod]
        public void MakeSafe_normal()
        {
            string name = "Kinematics";
            string safe = name.RemoveUnsafeCharacters();

            Assert.AreEqual("Kinematics", safe);
        }

        [TestMethod]
        public void MakeSafe_sentence()
        {
            string name = "Kinematics walks down the road.";
            string safe = name.RemoveUnsafeCharacters();

            Assert.AreEqual("Kinematics walks down the road.", safe);
        }

        [TestMethod]
        public void MakeSafe_nbsp()
        {
            // This sentence includes a non-breaking space.
            string name = "Kinematics walks down the road.";
            string safe = name.RemoveUnsafeCharacters();

            Assert.AreEqual("Kinematics walks down the road.", safe);
        }
        #endregion

        #region Agnostic hash comparisons
        [TestMethod]
        public void AgnosticHash_01_space()
        {
            int result1 = Agnostic.DefaultHashFunction("Kinematics", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("Kinematics ", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_02_extra_chars()
        {
            int result1 = Agnostic.DefaultHashFunction("Kinematics", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("Kinematicss", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreNotEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_03_diacritical()
        {
            int result1 = Agnostic.DefaultHashFunction("resume", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("resumé", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_04_diacritical()
        {
            int result1 = Agnostic.DefaultHashFunction("resume", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("resumé", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_05_fraction_form()
        {
            int result1 = Agnostic.DefaultHashFunction("Ranma ½", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("Ranma 1/2 ", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_05a_fraction_not_ignored()
        {
            int result1 = Agnostic.DefaultHashFunction("Ranma ½", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("Ranma", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreNotEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_06_punctuation()
        {
            int result1 = Agnostic.DefaultHashFunction("[bank]", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("bank ", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_07_capitalization()
        {
            int result1 = Agnostic.DefaultHashFunction("BANK", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("bank ", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_08_extra_letter()
        {
            int result1 = Agnostic.DefaultHashFunction("bahnk", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("bank ", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreNotEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_09_spacing()
        {
            int result1 = Agnostic.DefaultHashFunction("ban k", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("bank ", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void AgnosticHash_10_extra_number()
        {
            int result1 = Agnostic.DefaultHashFunction("runover", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);
            int result2 = Agnostic.DefaultHashFunction("run over 1", CultureInfo.InvariantCulture.CompareInfo, CompareOptions.None);

            Assert.AreNotEqual(result1, result2);
        }

        #endregion

    }
}
