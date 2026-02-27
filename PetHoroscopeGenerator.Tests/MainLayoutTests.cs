using Bunit;
using PetHoroscopeGenerator.Web.Components.Layout;

namespace PetHoroscopeGenerator.Tests
{
    [TestClass]
    public class MainLayoutTests
    {
        private Bunit.TestContext? _testContext;

        [TestInitialize]
        public void Setup()
        {
            _testContext = new Bunit.TestContext();
        }

        [TestMethod]
        public void MainLayout_RendersCorrectly()
        {
            // Arrange & Act
            var component = _testContext.RenderComponent<MainLayout>();

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
            _testContext.Dispose();
        }
    }
}
