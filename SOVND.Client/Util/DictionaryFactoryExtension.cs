using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SOVND.Client.Util
{
    public class DictionaryFactoryExtension : MarkupExtension, IDictionary
    {
        public Type KeyType { get; set; }
        public Type ValueType { get; set; }

        private IDictionary _dictionary;
        private object _syncRoot = new object();

        private IDictionary Dictionary
        {
            get
            {
                if (_dictionary == null)
                {
                    var type = typeof(Dictionary<,>);
                    var dictType = type.MakeGenericType(KeyType, ValueType);
                    _dictionary = (IDictionary)Activator.CreateInstance(dictType);
                }
                return _dictionary;
            }
        }


        public void Add(object key, object value)
        {
            if (!KeyType.IsAssignableFrom(key.GetType()))
                key = TypeDescriptor.GetConverter(KeyType).ConvertFrom(key);
            Dictionary.Add(key, value);
        }

        #region Other Interface Members
        public void Clear()
        {
            throw new NotSupportedException();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Remove(object key)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object key)
        {
            throw new NotSupportedException();
        }
        
        // <Many more members that do not matter one bit...>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var expando = (IDictionary<string, object>)new ExpandoObject();
            foreach (DictionaryEntry kvp in Dictionary)
                expando[(string)kvp.Key] = kvp.Value;

            return expando;
        }

        //The designer uses this for whatever reason...
        public object this[object key]
        {
            get { return this.Dictionary[key]; }
            set { this.Dictionary[key] = value; }
        }

        public ICollection Keys
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection Values
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return 1; }
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
            set { _syncRoot = value; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }
    }
}
