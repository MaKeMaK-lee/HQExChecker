using System.Collections.ObjectModel;

namespace HQExChecker.GUI.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void AddOrReplace<T>(this ObservableCollection<T> collection, T item, Func<T, bool> predicate) where T : class
        {
            var exists = collection.FirstOrDefault(predicate);
            if (exists != null)
            {
                if (exists.EqualProps(item))
                    return;
                collection.Remove(exists);
            }
            collection.Add(item);
        }
    }
}
