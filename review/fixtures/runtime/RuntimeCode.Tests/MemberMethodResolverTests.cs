using RuntimeCode;

namespace RuntimeCode.Tests
{
    public class TestBase
    {
        public string BaseProp { get; set; } = "";

        public bool ShouldSerializeBaseProp() => false;
    }

    public class Test : TestBase
    {
        public string TestProp { get; set; } = "";

        public bool ShouldSerializeTestProp() => false;
    }

    [TestClass]
    public class MemberMethodResolverTests
    {
        public class PersistBase
        {
            public bool ShouldSerializeBaseProp() => true;
        }

        public class PersistDerived : PersistBase
        {
            public bool ShouldSerializeOwnProp() => true;
        }

        [TestMethod]
        public void CheckShouldPersist_TrueMethodOnSameClass_PreservesTrue()
        {
            Assert.IsTrue(MemberMethodResolver.CheckShouldPersist(new PersistDerived(), "OwnProp"));
        }

        [TestMethod]
        public void CheckShouldPersist_TrueMethodOnBaseClass_PreservesTrue()
        {
            Assert.IsTrue(MemberMethodResolver.CheckShouldPersist(new PersistDerived(), "BaseProp"));
        }

        [TestMethod]
        public void CheckShouldPersist_MethodOnSameClass_ReturnsValue()
        {
            var obj = new Test();
            bool result = MemberMethodResolver.CheckShouldPersist(obj, "TestProp");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckShouldPersist_MethodOnBaseClassViaBaseInstance_Works()
        {
            var obj = new TestBase();
            bool result = MemberMethodResolver.CheckShouldPersist(obj, "BaseProp");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckShouldPersist_MethodOnBaseClass_NullDereference()
        {
            var obj = new Test();
            bool result = MemberMethodResolver.CheckShouldPersist(obj, "BaseProp");
            Assert.IsFalse(result);
        }
    }
}
