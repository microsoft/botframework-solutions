using AutomotiveSkill.Models;
using AutomotiveSkill.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AutomotiveSkillTest.Yaml
{
    [TestClass]
    public class YamlParseUtilTests
    {
        [TestMethod]
        public void Test_ParseDocumentAsNonGeneric()
        {
            string yaml = "unit: bar";

            AvailableSettingAmount expectedAmount = new AvailableSettingAmount
            {
                Unit = "bar",
            };

            using (TextReader reader = new StringReader(yaml))
            {
                var amount = YamlParseUtil.ParseDocument<AvailableSettingAmount>(reader);
                Assert.AreEqual(expectedAmount, amount);
            }
        }

        [TestMethod]
        public void Test_ParseDocumentAsList()
        {
            AvailableSetting foo = new AvailableSetting
            {
                CanonicalName = "Foo",
                Values = new List<AvailableSettingValue>
                {
                    new AvailableSettingValue
                    {
                        CanonicalName = "Set",
                        RequiresAmount = true,
                    },
                    new AvailableSettingValue
                    {
                        CanonicalName = "Decrease",
                        ChangesSignOfAmount = true,
                    },
                    new AvailableSettingValue
                    {
                        CanonicalName = "Increase",
                        Antonym = "Decrease",
                    },
                },
                AllowsAmount = true,
                Amounts = new List<AvailableSettingAmount>
                {
                    new AvailableSettingAmount
                    {
                        Unit = "bar",
                        Min = 14,
                        Max = 32,
                    },
                    new AvailableSettingAmount
                    {
                        Unit = "",
                        Min = -5,
                    },
                },
                IncludedSettings = new List<string>
                {
                    "Front Foo",
                    "Rear Foo",
                },
            };

            AvailableSetting qux = new AvailableSetting
            {
                CanonicalName = "Qux",
                Values = new List<AvailableSettingValue>
                {
                    new AvailableSettingValue
                    {
                        CanonicalName = "Off",
                        RequiresConfirmation = true,
                    },
                    new AvailableSettingValue
                    {
                        CanonicalName = "On",
                    },
                },
            };

            List<AvailableSetting> expectedAvailableSettings = new List<AvailableSetting>
            {
                foo,
                qux,
            };

            using (TextReader reader = GetTestResourceStream("test_available_settings.yaml"))
            {
                var availableSettings = YamlParseUtil.ParseDocument<List<AvailableSetting>>(reader);
                CollectionAssert.AreEqual(expectedAvailableSettings, availableSettings);
            }
        }

        [TestMethod]
        public void Test_ParseDocumentAsDictionaryWithNestedGenerics()
        {
            SettingAlternativeNames defaultAlternativeNames = new SettingAlternativeNames
            {
                AlternativeValueNames = new Dictionary<string, IList<string>>
                {
                    { "On", new List<string> { "enabled" } },
                    { "Off", new List<string> { "disabled" } },
                },
            };

            SettingAlternativeNames fooAlternativeNames = new SettingAlternativeNames
            {
                AlternativeNames = new List<string> { "fooing" },
            };

            Dictionary<string, SettingAlternativeNames> expectedAlternativeNameMap = new Dictionary<string, SettingAlternativeNames>
            {
                { "*DEFAULT*", defaultAlternativeNames },
                { "Foo", fooAlternativeNames },
            };

            using (TextReader reader = GetTestResourceStream("test_setting_alternative_names.yaml"))
            {
                var alternativeNameMap = YamlParseUtil.ParseDocument<Dictionary<string, SettingAlternativeNames>>(reader);
                Assert.AreEqual(expectedAlternativeNameMap.Count, alternativeNameMap.Count);
                Assert.IsTrue(expectedAlternativeNameMap.All(pair => alternativeNameMap.Contains(pair)));
            }
        }

        private StreamReader GetTestResourceStream(string fileName)
        {
            Assembly resourceAssembly = typeof(YamlParseUtilTests).Assembly;
            var filePath = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(fileName))
                .First();

            return new StreamReader(resourceAssembly.GetManifestResourceStream(filePath));
        }
    }
}
