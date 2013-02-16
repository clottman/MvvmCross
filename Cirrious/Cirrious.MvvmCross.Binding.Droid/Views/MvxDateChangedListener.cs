using Android.Widget;

namespace Cirrious.MvvmCross.Binding.Droid.Views
{
    public class MvxDateChangedListener
        : Java.Lang.Object
          , DatePicker.IOnDateChangedListener
    {
        private readonly IMvxDateListenerTarget _target;

        public MvxDateChangedListener(IMvxDateListenerTarget target)
        {
            _target = target;
        }

        public void OnDateChanged(DatePicker view, int year, int monthOfYear, int dayOfMonth)
        {
            var dateTime = MvxJavaDateUtils.DateTimeFromJava(year, monthOfYear, dayOfMonth);
            _target.InternalSetValueAndRaiseChanged(dateTime);
        }
    }
}