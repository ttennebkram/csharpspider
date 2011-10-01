using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;

namespace Spider {

    public class SpiderStatus : INotifyPropertyChanged {

        public string message;

        /* SpiderStatus() -     make a new SpiderStatus() object representing any kind of status update
         *                      from a Spider object
         *  @message -          the status update message
         */
        public SpiderStatus(string message) {
            this.message = message;
        }

        /* OnPropertyChanged() -    this will be called by the UI thread when the message property of
         *                          an object in the list of SpiderStatus objects that represents our
         *                          status updates changes; not to be called explicitly.
         */
        protected virtual void OnPropertyChanged(String propertyName) {
            var handler = this.PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Message {
            get {
                return this.message;
            }

            set {
                if (this.message != value) {
                    this.message = value;
                    this.OnPropertyChanged("Message");
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}