using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Threading;

namespace WpfApplication6
{
    class NotifyingDateTime:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DateTime _now;

        public NotifyingDateTime()
        {
            _now = DateTime.Now;

            DispatcherTimer timer = new DispatcherTimer();

            timer.Interval = TimeSpan.FromMilliseconds(100);

            timer.Tick += new EventHandler(timer_Tick);

            timer.Start();
        }

        public DateTime Now
        {
            get{return _now;}
            private set
            {
                _now = value;
                if(PropertyChanged!=null)
                PropertyChanged(this,new PropertyChangedEventArgs("Now"));
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Now = DateTime.Now;
        } 

    }
}
