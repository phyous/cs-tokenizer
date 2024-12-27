using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsTokenizer.Implementation;
using CsTokenizer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class ObjectPoolTests
    {
        private ObjectPool<StringBuilder> _pool = null!;
        private const int MaxSize = 5;

        [TestInitialize]
        public void Setup()
        {
            _pool = new ObjectPool<StringBuilder>(
                () => new StringBuilder(),
                sb => sb.Clear(),
                MaxSize);
        }

        [TestMethod]
        public void TestRentNewObject()
        {
            // Act
            var obj = _pool.Rent();

            // Assert
            Assert.IsNotNull(obj);
            Assert.AreEqual(0, obj.Length);
        }

        [TestMethod]
        public void TestReturnAndRent()
        {
            // Arrange
            var obj1 = _pool.Rent();
            obj1.Append("test");
            _pool.Return(obj1);

            // Act
            var obj2 = _pool.Rent();

            // Assert
            Assert.IsNotNull(obj2);
            Assert.AreEqual(0, obj2.Length); // Should be cleared by reset action
            Assert.IsTrue(ReferenceEquals(obj1, obj2)); // Should get same object back
        }

        [TestMethod]
        public void TestMaxSize()
        {
            // Arrange
            var objects = new List<StringBuilder>();
            for (int i = 0; i < MaxSize + 1; i++)
            {
                objects.Add(_pool.Rent());
            }

            // Act - Return all objects
            foreach (var obj in objects)
            {
                _pool.Return(obj);
            }

            // Get MaxSize + 1 objects again
            var newObjects = new List<StringBuilder>();
            for (int i = 0; i < MaxSize + 1; i++)
            {
                newObjects.Add(_pool.Rent());
            }

            // Assert - Only MaxSize objects should be reused
            var reusedCount = objects.Count(o1 => newObjects.Any(o2 => ReferenceEquals(o1, o2)));
            Assert.AreEqual(MaxSize, reusedCount);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestReturnNull()
        {
            _pool.Return(null!);
        }

        [TestMethod]
        public void TestTokenListPool()
        {
            // Arrange & Act
            var list1 = TokenListPool.Rent();
            list1.Add(new Token("test", 1));
            TokenListPool.Return(list1);

            var list2 = TokenListPool.Rent();

            // Assert
            Assert.IsNotNull(list2);
            Assert.AreEqual(0, list2.Count); // Should be cleared
            Assert.IsTrue(ReferenceEquals(list1, list2)); // Should be same instance
        }

        [TestMethod]
        public void TestStringBuilderPool()
        {
            // Arrange & Act
            var sb1 = StringBuilderPool.Rent();
            sb1.Append("test");
            StringBuilderPool.Return(sb1);

            var sb2 = StringBuilderPool.Rent();

            // Assert
            Assert.IsNotNull(sb2);
            Assert.AreEqual(0, sb2.Length); // Should be cleared
            Assert.IsTrue(ReferenceEquals(sb1, sb2)); // Should be same instance
        }

        [TestMethod]
        public void TestConcurrentAccess()
        {
            // Arrange
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var obj = _pool.Rent();
                    obj.Append("test");
                    _pool.Return(obj);
                }));
            }

            // Assert - No exceptions should occur
            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void TestObjectReuse()
        {
            // Arrange
            var usedObjects = new HashSet<StringBuilder>();

            // Act
            for (int i = 0; i < MaxSize * 2; i++)
            {
                var obj = _pool.Rent();
                usedObjects.Add(obj);
                _pool.Return(obj);
            }

            // Assert
            Assert.IsTrue(usedObjects.Count <= MaxSize);
        }

        [TestMethod]
        public void TestResetAction()
        {
            // Arrange
            var resetCount = 0;
            var pool = new ObjectPool<StringBuilder>(
                () => new StringBuilder(),
                _ => resetCount++,
                MaxSize);

            var obj = pool.Rent();

            // Act
            pool.Return(obj);

            // Assert
            Assert.AreEqual(1, resetCount);
        }
    }
} 