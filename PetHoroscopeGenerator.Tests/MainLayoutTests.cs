using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PetHoroscopeGenerator.Web.Components.Layout;

namespace PetHoroscopeGenerator.Tests
{
    [TestClass]
    public class MainLayoutTests
    {
        private TestHost _host;

        [TestInitialize]
        public void Setup()
        {
            _host = new TestHost();
        }

        [TestMethod]
        public void MainLayout_RendersCorrectly()
        {
            // Arrange & Act
            var component = _host.AddComponent<MainLayout>();

            // Assert
            Assert.IsNotNull(component);
            var header = component.Find("header");
            Assert.IsNotNull(header);
            var logo = component.Find("#logo");
            Assert.IsNotNull(logo);
            var title = component.Find("h1");
            Assert.AreEqual("Pet Horoscope Generator", title.InnerHtml);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _host.Dispose();
        }
    }
}
