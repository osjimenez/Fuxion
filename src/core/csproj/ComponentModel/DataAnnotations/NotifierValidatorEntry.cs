﻿using System;

namespace Fuxion.ComponentModel.DataAnnotations
{
    public class NotifierValidatorMessage : Notifier<NotifierValidatorMessage>
    {
        public NotifierValidatorMessage(object @object)
        {
            Object = @object;
        }
        internal object Object { get; set; }
		internal Func<string> PathFunc { get; set; }
		public string Path => PathFunc();
        public string PropertyName
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }
        public string PropertyDisplayName
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }
        public string Message
        {
            get => GetValue<string>();
            internal set => SetValue(value);
        }

        public override bool Equals(object obj)
        {
            if (obj is NotifierValidatorMessage ve)
                return ve.Object == Object && ve.Path == Path && ve.PropertyName == PropertyName && ve.Message == Message;
            return base.Equals(obj);
        }
        public override int GetHashCode() => Object.GetHashCode() ^ Path.GetHashCode() ^ PropertyName.GetHashCode() ^ Message.GetHashCode();

        public override string ToString() => Message;
    }
    //public class ValidatorEntryCollection : INotifyCollectionChanged
    //{
    //    public event NotifyCollectionChangedEventHandler CollectionChanged;

    //    public ReadOnlyObservableCollection<ValidatorEntryNamedCollection> ByPath { get; set; }
    //    public ReadOnlyObservableCollection<ValidatorEntryNamedCollection> ByPropertyName { get; set; }
    //}
    //public class ValidatorEntryNamedCollection : ValidatorEntryCollection, INotifyCollectionChanged
    //{
    //    public string Name { get; set; }
    //}
}