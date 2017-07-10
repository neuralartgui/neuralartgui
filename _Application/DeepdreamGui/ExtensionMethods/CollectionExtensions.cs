using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeepdreamGui.ExtensionMethods
{
    /// <summary>
    /// Extension Methods for Collections
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Convert IEnumerable to Observable Collection
        /// </summary>
        /// <typeparam name="T">Content Type</typeparam>
        /// <param name="col">Base Collection</param>
        /// <returns>Observable Collection</returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> col)
        {
            return new ObservableCollection<T>(col);
        }

        /// <summary>
        /// Add Element to collection on UI thread. Prevents Threading ownership exceptions.
        /// </summary>
        /// <typeparam name="T">Content Type</typeparam>
        /// <param name="collection">Collection to add item to</param>
        /// <param name="item">Item to add</param>
        public static void AddOnUi<T>(this ICollection<T> collection, T item)
        {
            Action<T> addMethod = collection.Add;
            Application.Current.Dispatcher.BeginInvoke(addMethod, item);
        }
    }
}
