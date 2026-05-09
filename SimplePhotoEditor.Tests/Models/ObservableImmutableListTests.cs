using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using SimplePhotoEditor.Models;
using Xunit;

namespace SimplePhotoEditor.Tests.Models
{
    public class ObservableImmutableListTests
    {
        [Fact]
        public void DefaultConstructor_CreatesEmptyList()
        {
            var list = new ObservableImmutableList<int>();

            Assert.Equal(0, list.Count);
            Assert.False(list.IsReadOnly);
            Assert.False(list.IsFixedSize);
            Assert.False(list.IsSynchronized);
            Assert.NotNull(list.SyncRoot);
        }

        [Fact]
        public void Constructor_FromEnumerable_PopulatesList()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);
        }

        [Fact]
        public void Constructor_WithLockType_DoesNotThrow()
        {
            var spinList = new ObservableImmutableList<int>(ObservableCollectionObject.LockTypeEnum.SpinWait);
            var lockList = new ObservableImmutableList<int>(ObservableCollectionObject.LockTypeEnum.Lock);

            Assert.Equal(ObservableCollectionObject.LockTypeEnum.SpinWait, spinList.LockType);
            Assert.Equal(ObservableCollectionObject.LockTypeEnum.Lock, lockList.LockType);
        }

        [Fact]
        public void Add_AppendsValueAndRaisesNotification()
        {
            var list = new ObservableImmutableList<int>();
            NotifyCollectionChangedAction? action = null;
            int? newIndex = null;
            list.CollectionChanged += (_, e) =>
            {
                action = e.Action;
                newIndex = e.NewStartingIndex;
            };

            list.Add(42);

            Assert.Equal(1, list.Count);
            Assert.Equal(42, list[0]);
            Assert.Equal(NotifyCollectionChangedAction.Add, action);
            Assert.Equal(0, newIndex);
        }

        [Fact]
        public void AddRange_AppendsAllValues()
        {
            var list = new ObservableImmutableList<int>(new[] { 1 });

            list.AddRange(new[] { 2, 3, 4 });

            Assert.Equal(new[] { 1, 2, 3, 4 }, list);
        }

        [Fact]
        public void Insert_AddsItemAtIndex()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 3 });

            list.Insert(1, 2);

            Assert.Equal(new[] { 1, 2, 3 }, list);
        }

        [Fact]
        public void InsertRange_AddsItemsAtIndex()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 4 });

            list.InsertRange(1, new[] { 2, 3 });

            Assert.Equal(new[] { 1, 2, 3, 4 }, list);
        }

        [Fact]
        public void Clear_EmptiesList()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            list.Clear();

            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void Contains_ReturnsExpected()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            Assert.True(list.Contains(2));
            Assert.False(list.Contains(99));
        }

        [Fact]
        public void IndexOf_ReturnsExpected()
        {
            var list = new ObservableImmutableList<int>(new[] { 10, 20, 30 });

            Assert.Equal(0, list.IndexOf(10));
            Assert.Equal(2, list.IndexOf(30));
            Assert.Equal(-1, list.IndexOf(99));
        }

        [Fact]
        public void IndexOf_WithRange_ReturnsExpected()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3, 2 });

            var idx = list.IndexOf(2, 2, 2, EqualityComparer<int>.Default);

            Assert.Equal(3, idx);
        }

        [Fact]
        public void LastIndexOf_WithRange_ReturnsExpected()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3, 2 });

            var idx = list.LastIndexOf(2, 3, 4, EqualityComparer<int>.Default);

            Assert.Equal(3, idx);
        }

        [Fact]
        public void Remove_ReturnsTrueWhenItemPresent()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var removed = list.Remove(2);

            Assert.True(removed);
            Assert.Equal(new[] { 1, 3 }, list);
        }

        [Fact]
        public void Remove_ReturnsFalseWhenItemAbsent()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var removed = list.Remove(99);

            Assert.False(removed);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void RemoveAt_RemovesItemAtIndex()
        {
            var list = new ObservableImmutableList<int>(new[] { 10, 20, 30 });

            list.RemoveAt(1);

            Assert.Equal(new[] { 10, 30 }, list);
        }

        [Fact]
        public void RemoveAll_RemovesMatchingItems()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3, 4, 5 });

            list.RemoveAll(x => x % 2 == 0);

            Assert.Equal(new[] { 1, 3, 5 }, list);
        }

        [Fact]
        public void RemoveRange_ByIndex_RemovesContiguousItems()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3, 4, 5 });

            list.RemoveRange(1, 2);

            Assert.Equal(new[] { 1, 4, 5 }, list);
        }

        [Fact]
        public void RemoveRange_ByEnumerable_RemovesEachItem()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3, 4, 5 });

            list.RemoveRange(new[] { 2, 4 }, EqualityComparer<int>.Default);

            Assert.Equal(new[] { 1, 3, 5 }, list);
        }

        [Fact]
        public void Replace_SwapsValueWithEqualityComparer()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            list.Replace(2, 99, EqualityComparer<int>.Default);

            Assert.Equal(new[] { 1, 99, 3 }, list);
        }

        [Fact]
        public void SetItem_ReplacesElementAndRaisesReplaceNotification()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });
            NotifyCollectionChangedAction? action = null;
            list.CollectionChanged += (_, e) => action = e.Action;

            list.SetItem(1, 99);

            Assert.Equal(99, list[1]);
            Assert.Equal(NotifyCollectionChangedAction.Replace, action);
        }

        [Fact]
        public void Indexer_Set_ReplacesElement()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            list[1] = 99;

            Assert.Equal(99, list[1]);
        }

        [Fact]
        public void IListAdd_ReturnsIndexOfInserted()
        {
            IList list = new ObservableImmutableList<int>();

            var index = list.Add(5);

            Assert.Equal(0, index);
            Assert.Equal(5, list[0]);
        }

        [Fact]
        public void IListContains_DelegatesToTypedContains()
        {
            IList list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            Assert.True(list.Contains(2));
        }

        [Fact]
        public void CopyTo_CopiesAllItemsToArray()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });
            var array = new int[5];

            list.CopyTo(array, 1);

            Assert.Equal(new[] { 0, 1, 2, 3, 0 }, array);
        }

        [Fact]
        public void IListCopyTo_CopiesAllItemsToArray()
        {
            IList list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });
            var array = new int[3];

            list.CopyTo(array, 0);

            Assert.Equal(new[] { 1, 2, 3 }, array);
        }

        [Fact]
        public void GetEnumerator_IteratesAllItems()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var collected = new List<int>();
            foreach (var i in list)
            {
                collected.Add(i);
            }

            Assert.Equal(new[] { 1, 2, 3 }, collected);
        }

        [Fact]
        public void ToImmutableList_ReturnsCurrentSnapshot()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            ImmutableList<int> snapshot = list.ToImmutableList();

            Assert.Equal(new[] { 1, 2, 3 }, snapshot);
        }

        [Fact]
        public void DoAdd_AppliesValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 10 });

            var result = list.DoAdd(current => current[0] + 5);

            Assert.True(result);
            Assert.Equal(new[] { 10, 15 }, list);
        }

        [Fact]
        public void DoAddRange_AppliesValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1 });

            var result = list.DoAddRange(_ => new[] { 2, 3, 4 });

            Assert.True(result);
            Assert.Equal(new[] { 1, 2, 3, 4 }, list);
        }

        [Fact]
        public void DoInsert_AppliesValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 3 });

            var result = list.DoInsert(_ => new KeyValuePair<int, int>(1, 2));

            Assert.True(result);
            Assert.Equal(new[] { 1, 2, 3 }, list);
        }

        [Fact]
        public void DoRemove_RemovesItemFromValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var result = list.DoRemove(_ => 2);

            Assert.True(result);
            Assert.Equal(new[] { 1, 3 }, list);
        }

        [Fact]
        public void DoRemoveAt_RemovesItemFromValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var result = list.DoRemoveAt(_ => 0);

            Assert.True(result);
            Assert.Equal(new[] { 2, 3 }, list);
        }

        [Fact]
        public void DoSetItem_ReplacesItemFromValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var result = list.DoSetItem(_ => new KeyValuePair<int, int>(1, 99));

            Assert.True(result);
            Assert.Equal(new[] { 1, 99, 3 }, list);
        }

        [Fact]
        public void TryAdd_AppliesValueProvider()
        {
            var list = new ObservableImmutableList<int>();

            var result = list.TryAdd(_ => 5);

            Assert.True(result);
            Assert.Equal(new[] { 5 }, list);
        }

        [Fact]
        public void TryInsert_AppliesValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 3 });

            var result = list.TryInsert(_ => new KeyValuePair<int, int>(1, 2));

            Assert.True(result);
            Assert.Equal(new[] { 1, 2, 3 }, list);
        }

        [Fact]
        public void TryAddRange_AppliesValueProvider()
        {
            var list = new ObservableImmutableList<int>(new[] { 1 });

            var result = list.TryAddRange(_ => new[] { 2, 3 });

            Assert.True(result);
            Assert.Equal(new[] { 1, 2, 3 }, list);
        }

        [Fact]
        public void TryRemoveAt_RemovesItem()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var result = list.TryRemoveAt(_ => 1);

            Assert.True(result);
            Assert.Equal(new[] { 1, 3 }, list);
        }

        [Fact]
        public void TrySetItem_ReplacesItem()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });

            var result = list.TrySetItem(_ => new KeyValuePair<int, int>(2, 30));

            Assert.True(result);
            Assert.Equal(new[] { 1, 2, 30 }, list);
        }

        [Fact]
        public void TryOperation_ReturningNullCancelsWithoutMutation()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });
            var snapshot = list.ToImmutableList();

            var result = list.TryOperation(_ => null);

            Assert.False(result);
            Assert.Equal(snapshot, list.ToImmutableList());
        }

        [Fact]
        public void DoOperation_ReturningNullCancelsWithoutMutation()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });
            var snapshot = list.ToImmutableList();

            var result = list.DoOperation(_ => null);

            Assert.False(result);
            Assert.Equal(snapshot, list.ToImmutableList());
        }

        [Fact]
        public void DoOperation_AppliesUpdatedListAndRaisesNotification()
        {
            var list = new ObservableImmutableList<int>(new[] { 1, 2, 3 });
            NotifyCollectionChangedAction? action = null;
            list.CollectionChanged += (_, e) => action = e.Action;

            var result = list.DoOperation(current => current.Add(4));

            Assert.True(result);
            Assert.Equal(new[] { 1, 2, 3, 4 }, list);
            Assert.Equal(NotifyCollectionChangedAction.Reset, action);
        }
    }
}
