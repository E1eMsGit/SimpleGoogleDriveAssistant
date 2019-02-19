using System;
using System.ComponentModel;

namespace SimpleGoogleDriveAssistant.Models
{
    class MenuItemModel : INotifyPropertyChanged
    {
        public MenuItemModel(string name, object content, string title)
        {
            _name = name;
            Content = content;
            _title = title;
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); }
        }


        private object _content;
        public object Content
        {
            get => _content;
            set { _content = value; RaisePropertyChanged(); }
        }


        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; RaisePropertyChanged(); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private Action<PropertyChangedEventArgs> RaisePropertyChanged()
        {
            return args => PropertyChanged?.Invoke(this, args);
        }
    }
}
