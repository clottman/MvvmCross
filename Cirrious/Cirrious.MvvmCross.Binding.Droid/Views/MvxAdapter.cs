// MvxAdapter.cs
// (c) Copyright Cirrious Ltd. http://www.cirrious.com
// MvvmCross is licensed using Microsoft Public License (Ms-PL)
// Contributions and inspirations noted in readme.md and license.txt
// 
// Project Lead - Stuart Lodge, @slodge, me@slodge.com

using System;
using System.Collections;
using System.Collections.Specialized;
using Android;
using Android.Content;
using Android.Views;
using Android.Widget;
using Cirrious.CrossCore.Exceptions;
using Cirrious.CrossCore.Interfaces.IoC;
using Cirrious.CrossCore.Interfaces.Platform.Diagnostics;
using Cirrious.MvvmCross.Binding.Attributes;
using Cirrious.MvvmCross.Binding.Droid.Interfaces.BindingContext;
using Cirrious.MvvmCross.Binding.Droid.Interfaces.Views;
using Cirrious.MvvmCross.Binding.ExtensionMethods;
using Cirrious.MvvmCross.Binding.Interfaces.BindingContext;
using Object = Java.Lang.Object;

namespace Cirrious.MvvmCross.Binding.Droid.Views
{
    public class MvxAdapter
        : BaseAdapter
    {
        private readonly IMvxBindingContext _bindingContext;
        private readonly Context _context;
        private int _itemTemplateId;
        private int _dropDownItemTemplateId;
        private IEnumerable _itemsSource;
        private IDisposable _subscription;

        public MvxAdapter(Context context)
            : this(context, null)
        {
        }

        public MvxAdapter(Context context, IMvxBindingContext bindingContext)
        {
            _context = context;
            _bindingContext = bindingContext;
            if (_bindingContext == null)
            {
                var stack = Mvx.Resolve<IMvxBindingContextStack<IMvxBindingContext>>();
                _bindingContext = stack.Current;
            }
            if (_bindingContext == null)
                throw new MvxException(
                    "MvxListView can only be used within a Context which supports IMvxBindingContext");
            SimpleViewLayoutId = Resource.Layout.SimpleListItem1;
        }

        protected Context Context
        {
            get { return _context; }
        }

        protected IMvxBindingContext BindingContext
        {
            get { return _bindingContext; }
        }

        public int SimpleViewLayoutId { get; set; }

        [MvxSetToNullAfterBinding]
        public IEnumerable ItemsSource
        {
            get { return _itemsSource; }
            set { SetItemsSource(value); }
        }

        public int ItemTemplateId
        {
            get { return _itemTemplateId; }
            set
            {
                if (_itemTemplateId == value)
                    return;
                _itemTemplateId = value;

                // since the template has changed then let's force the list to redisplay by firing NotifyDataSetChanged()
                if (_itemsSource != null)
                    NotifyDataSetChanged();
            }
        }

        public int DropDownItemTemplateId
        {
            get { return _dropDownItemTemplateId; }
            set
            {
                if (_dropDownItemTemplateId == value)
                    return;
                _dropDownItemTemplateId = value;

                // since the template has changed then let's force the list to redisplay by firing NotifyDataSetChanged()
                if (_itemsSource != null)
                    NotifyDataSetChanged();
            }
        }

        public override int Count
        {
            get { return _itemsSource.Count(); }
        }

        protected virtual void SetItemsSource(IEnumerable value)
        {
            if (_itemsSource == value)
                return;

            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
            _itemsSource = value;
            if (_itemsSource != null && !(_itemsSource is IList))
                MvxBindingTrace.Trace(MvxTraceLevel.Warning,
                                      "Binding to IEnumerable rather than IList - this can be inefficient, especially for large lists");
            var newObservable = _itemsSource as INotifyCollectionChanged;
            if (newObservable != null)
            {
                _subscription = newObservable.WeakSubscribe(OnItemsSourceCollectionChanged);
            }
            NotifyDataSetChanged();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyDataSetChanged(e);
        }

        public virtual void NotifyDataSetChanged(NotifyCollectionChangedEventArgs e)
        {
            base.NotifyDataSetChanged();
        }

        public int GetPosition(object item)
        {
            return _itemsSource.GetPosition(item);
        }

        public virtual System.Object GetRawItem(int position)
        {
            return _itemsSource.ElementAt(position);
        }

        public override Object GetItem(int position)
        {
            // we return null to Java here
            // - see @JonPryor's answer in http://stackoverflow.com/questions/13842864/why-does-the-gref-go-too-high-when-i-put-a-mvxbindablespinner-in-a-mvxbindableli/13995199#comment19319057_13995199
            return null;
            //return new MvxJavaContainer<object>(GetRawItem(position));
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            return GetView(position, convertView, parent, DropDownItemTemplateId);
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            return GetView(position, convertView, parent, ItemTemplateId);
        }

        protected virtual View GetView(int position, View convertView, ViewGroup parent, int templateId)
        {
            if (_itemsSource == null)
            {
                MvxBindingTrace.Trace(MvxTraceLevel.Error, "GetView called when ItemsSource is null");
                return null;
            }

            // 15 Oct 2012 - this position check removed as it's inefficient, especially for IEnumerable based collections
            /*
            if (position < 0 || position >= Count)
            {
                MvxBindingTrace.Trace(MvxTraceLevel.Error, "GetView called with invalid Position - zero indexed {0} out of {1}", position, Count);
                return null;
            }
            */

            var source = GetRawItem(position);

            return GetBindableView(convertView, source, templateId);
        }

        protected virtual View GetSimpleView(View convertView, object dataContext)
        {
            if (convertView == null)
            {
                convertView = CreateSimpleView(dataContext);
            }
            else
            {
                BindSimpleView(convertView, dataContext);
            }

            return convertView;
        }

        protected virtual void BindSimpleView(View convertView, object dataContext)
        {
            var textView = convertView as TextView;
            if (textView != null)
            {
                textView.Text = (dataContext ?? string.Empty).ToString();
            }
        }

        protected virtual View CreateSimpleView(object dataContext)
        {
            // note - this could technically be a non-binding inflate - but the overhead is minial
            var view = _bindingContext.BindingInflate(SimpleViewLayoutId, null);
            BindSimpleView(view, dataContext);
            return view;
        }

        protected virtual View GetBindableView(View convertView, object dataContext)
        {
            return GetBindableView(convertView, dataContext, ItemTemplateId);
        }

        protected virtual View GetBindableView(View convertView, object dataContext, int templateId)
        {
            if (templateId == 0)
            {
                // no template seen - so use a standard string view from Android and use ToString()
                return GetSimpleView(convertView, dataContext);
            }

            // we have a templateid so lets use bind and inflate on it :)
            var viewToUse = convertView as IMvxListItemView;
            if (viewToUse != null)
            {
                if (viewToUse.TemplateId != templateId)
                {
                    viewToUse = null;
                }
            }

            if (viewToUse == null)
            {
                viewToUse = CreateBindableView(dataContext, templateId);
            }
            else
            {
                BindBindableView(dataContext, viewToUse);
            }

            return viewToUse as View;
        }

        protected virtual void BindBindableView(object source, IMvxListItemView viewToUse)
        {
            viewToUse.DataContext = source;
        }

        protected virtual MvxListItemView CreateBindableView(object dataContext, int templateId)
        {
            return new MvxListItemView(_context, _bindingContext.LayoutInflater, dataContext, templateId);
        }
    }
}