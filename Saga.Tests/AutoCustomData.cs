using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace Saga.Tests {
    public class AutoCustomDataAttribute : AutoDataAttribute
    {
        public AutoCustomDataAttribute() : base(
            () => new Fixture()
                .Customize(new AutoMoqCustomization()))
        {
        }
    }
}