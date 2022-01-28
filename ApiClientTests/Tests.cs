using System.Collections.Generic;
using System.Linq;
using MyApi;
using NUnit.Framework;

namespace ApiClientTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ShouldBeAbleToGetMyTypeVariantObjectFromCollectionOfIMyTypes()
        {
            IList<IMyType> myTypes = new List<IMyType>()
            {
                new MyType {BaseProp1 = "Alpha", BaseProp2 = "Bravo", BaseProp3 = "Charlie"},
                new MyTypeVariant {BaseProp1 = "Delta", BaseProp2 = "Echo", BaseProp3 = "Foxtrot", DerivedPropA = "Golf"}
            };

            var variants = myTypes.Where(o => o.Type == MyTypeBaseType.MyTypeVariant).Cast<MyTypeVariant>();

            Assert.That(variants.Count(), Is.EqualTo(1));
            Assert.That(variants.First(), Is.SameAs(myTypes[1]));
        }
    }
}