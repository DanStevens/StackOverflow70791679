using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MyApi;
using NUnit.Framework;

namespace ApiClientTests
{
    public class Tests
    {
        private readonly Base[] _allObjects = {
            new Base {BaseProp1 = "Alpha", BaseProp2 = "Bravo", BaseProp3 = "Charlie"},
            new Derived {BaseProp1 = "Delta", BaseProp2 = "Echo", BaseProp3 = "Foxtrot", DerivedPropA = "Golf"}
        };

        [Test]
        public void ShouldBeAbleToAccessPropertiesOnBaseAndDerivedTypes()
        {
            var baseObject = _allObjects[0];
            Assert.That(baseObject, Is.TypeOf<Base>());
            Assert.That(baseObject.BaseProp1, Is.EqualTo("Alpha"));

            var derivedObject = (Derived)_allObjects[1];
            Assert.That(derivedObject, Is.TypeOf<Derived>());
            Assert.That(derivedObject.DerivedPropA, Is.EqualTo("Golf"));
        }

        [Test]
        public void ShouldBeAbleToDiscriminateDerivativeTypesUsingTypeCasting()
        {
            var derivatives = _allObjects.OfType<Derived>().ToArray();
            Assert.That(derivatives.Length, Is.EqualTo(1));
            Assert.That(derivatives[0], Is.SameAs(_allObjects[1]));
        }


        [Ignore("Example usage only - API host doesn't exist")]
        [Test]
        public async Task TestGetAllOperation()
        {
            using var httpClient = new HttpClient();
            IMyApiClient apiClient = new MyApiClient("https://example.io/", httpClient);
            var resp = await apiClient.GetAllAsync();
            Assert.That(resp, Is.TypeOf<ICollection<Base>>());

            var allObjects = resp.ToArray();
            Assert.That(allObjects.Length, Is.EqualTo(2));
            Assert.That(allObjects[0].BaseProp1, Is.EqualTo("Alpha"));
            Assert.That(((Derived)allObjects[1]).DerivedPropA, Is.EqualTo("Golf"));
        }
    }
}