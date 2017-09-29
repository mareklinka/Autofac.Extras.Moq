using System;
using System.Linq;
using System.Reflection;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Moq;
using Xunit;

namespace Autofac.Extras.Moq.Test
{
    public class AutoMockFixture
    {
        [Fact]
        public void AbstractDependencyIsFulfilled()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var component = mock.Create<TestComponentRequiringAbstractClassA>();
                Assert.Equal(
                    mock.Mock<AbstractClassA>().Object,
                    component.InstanceOfAbstractClassA);
            }
        }

        [Fact]
        public void RegularClassDependencyIsFulfilled()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var component = mock.Create<TestComponentRequiringClassA>();
                Assert.Equal(
                    mock.Mock<ClassA>().Object,
                    component.InstanceOfClassA);
            }
        }

        [Fact]
        public void DefaultConstructorIsLoose()
        {
            using (var mock = AutoMock.GetLoose())
            {
                RunWithSingleSetupationTest(mock);
            }
        }

        [Fact]
        public void DefaultConstructorWorksWithAllTests()
        {
            using (var mock = AutoMock.GetLoose())
            {
                RunTest(mock);
            }
        }

        [Fact]
        public void GetFromRepositoryUsesLooseBehaviorSetOnRepository()
        {
            using (var mock = AutoMock.GetFromRepository(new MockRepository(MockBehavior.Loose)))
            {
                RunWithSingleSetupationTest(mock);
            }
        }

        [Fact]
        public void GetFromRepositoryUsesStrictBehaviorSetOnRepository()
        {
            using (var mock = AutoMock.GetFromRepository(new MockRepository(MockBehavior.Strict)))
            {
                Assert.Throws<MockException>(() => RunWithSingleSetupationTest(mock));
            }
        }

        [Fact]
        public void LooseWorksWithUnmetSetupations()
        {
            using (var loose = AutoMock.GetLoose())
            {
                RunWithSingleSetupationTest(loose);
            }
        }

        [Fact]
        public void NormalSetupationsAreNotVerifiedByDefault()
        {
            using (var mock = AutoMock.GetLoose())
            {
                SetUpSetupations(mock);
            }
        }

        [Fact]
        public void ProperInitializationIsPerformed()
        {
            AssertProperties(AutoMock.GetLoose());
            AssertProperties(AutoMock.GetStrict());
        }

        [Fact]
        public void ProvideImplementation()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var serviceA = mock.Provide<IServiceA, ServiceA>();

                Assert.NotNull(serviceA);
                Assert.False(serviceA is IMocked<IServiceA>);
            }
        }

        [Fact]
        public void ProvideInstance()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var mockA = new Mock<IServiceA>();
                mockA.Setup(x => x.RunA());
                mock.Provide(mockA.Object);

                var component = mock.Create<TestComponent>();
                component.RunAll();

                mockA.VerifyAll();
            }
        }

        [Fact]
        public void StrictWorksWithAllSetupationsMet()
        {
            using (var strict = AutoMock.GetStrict())
            {
                RunTest(strict);
            }
        }

        [Fact]
        public void UnmetSetupationWithStrictMocksThrowsException()
        {
            using (var mock = AutoMock.GetStrict())
            {
                Assert.Throws<MockException>(() => RunWithSingleSetupationTest(mock));
            }
        }

        [Fact]
        public void UnmetVerifiableSetupationsCauseExceptionByDefault()
        {
            Assert.Throws<MockException>(() =>
                {
                    using (var mock = AutoMock.GetLoose())
                    {
                        SetUpVerifableSetupations(mock);
                    }
                });
        }

        [Fact]
        public void VerifyAllSetTrue_SetupationsAreVerified()
        {
            using (var mock = AutoMock.GetLoose())
            {
                mock.VerifyAll = true;
                RunTest(mock);
            }
        }

        [Fact]
        public void VerifyAllSetTrue_UnmetSetupationsCauseException()
        {
            Assert.Throws<MockException>(() =>
                {
                    using (var mock = AutoMock.GetLoose())
                    {
                        mock.VerifyAll = true;
                        SetUpSetupations(mock);
                    }
                });
        }

        [Fact]
        public void DefaultConstructorFinderForProtectedConstructorCreateThrows()
        {
            Assert.Throws<DependencyResolutionException>(() =>
                   {
                       using (var mock = AutoMock.GetLoose())
                       {
                           var component = mock.Create<ClassB>();
                       }
                   });
        }

        [Fact]
        public void DefaultConstructorFinderForProtectedConstructorProvideThrows()
        {
            Assert.Throws<DependencyResolutionException>(() =>
                   {
                       using (var mock = AutoMock.GetLoose())
                       {
                           mock.Provide<ClassB, ClassB>();
                       }
                   });
        }

        [Fact]
        public void CustomConstructorFinderForProtectedConstructorCreateWorks()
        {
            using (var mock = AutoMock.GetLoose(new BindingFlagsConstructorFinder(BindingFlags.NonPublic)))
            {
                var component = mock.Create<ClassB>();
                Assert.NotNull(component);
            }
        }

        [Fact]
        public void CustomConstructorFinderForProtectedConstructorProvideWorks()
        {
            using (var mock = AutoMock.GetLoose(new BindingFlagsConstructorFinder(BindingFlags.NonPublic)))
            {
                 mock.Provide<ClassB, ClassB>();
            }
        }

        private static void AssertProperties(AutoMock mock)
        {
            Assert.NotNull(mock.Container);
            Assert.NotNull(mock.MockRepository);
        }

        private static void RunTest(AutoMock mock)
        {
            SetUpSetupations(mock);

            var component = mock.Create<TestComponent>();
            component.RunAll();
        }

        private static void RunWithSingleSetupationTest(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB());

            var component = mock.Create<TestComponent>();
            component.RunAll();
        }

        private static void SetUpSetupations(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB());
            mock.Mock<IServiceA>().Setup(x => x.RunA());
        }

        private static void SetUpVerifableSetupations(AutoMock mock)
        {
            mock.Mock<IServiceB>().Setup(x => x.RunB()).Verifiable();
            mock.Mock<IServiceA>().Setup(x => x.RunA()).Verifiable();
        }

        public interface IServiceA
        {
            void RunA();
        }

        public interface IServiceB
        {
            void RunB();
        }

        public abstract class AbstractClassA
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class ClassA : AbstractClassA
        {
        }

        public class ClassB
        {
            protected ClassB()
            {
            }
        }

        public class ClassC
        {
            public ClassC(ClassB b)
            {
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public class ServiceA : IServiceA
        {
            public void RunA()
            {
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponent
        {
            private readonly IServiceA _serviceA;

            private readonly IServiceB _serviceB;

            public TestComponent(IServiceA serviceA, IServiceB serviceB)
            {
                this._serviceA = serviceA;
                this._serviceB = serviceB;
            }

            public void RunAll()
            {
                this._serviceA.RunA();
                this._serviceB.RunB();
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponentRequiringAbstractClassA
        {
            public TestComponentRequiringAbstractClassA(AbstractClassA abstractClassA)
            {
                this.InstanceOfAbstractClassA = abstractClassA;
            }

            public AbstractClassA InstanceOfAbstractClassA { get; }
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public sealed class TestComponentRequiringClassA
        {
            public TestComponentRequiringClassA(ClassA classA)
            {
                this.InstanceOfClassA = classA;
            }

            public ClassA InstanceOfClassA { get; }
        }

        public class BindingFlagsConstructorFinder : IConstructorFinder
        {
            private readonly BindingFlags _bindingFlags;

            public BindingFlagsConstructorFinder(BindingFlags bindingFlags)
            {
                _bindingFlags = bindingFlags;
            }

            public ConstructorInfo[] FindConstructors(Type targetType)
            {
                return targetType.FindMembers(
                                MemberTypes.Constructor,
                                BindingFlags.Instance | _bindingFlags,
                                null,
                                null).Cast<ConstructorInfo>().ToArray();
            }
        }
    }
}
