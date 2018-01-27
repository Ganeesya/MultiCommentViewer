﻿using GalaSoft.MvvmLight;
using SitePlugin;
//TODO:過去コメントの取得


namespace MultiCommentViewer
{
    public class MetadataViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }
        private string _active;
        public string Active
        {
            get { return _active; }
            set
            {
                _active = value;
                RaisePropertyChanged();
            }
        }

        public string ConnectionName => _connectionName.Name;
        private readonly ConnectionName _connectionName;
        public MetadataViewModel(ConnectionName connectionName)
        {
            _connectionName = connectionName;
            _connectionName.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(SitePlugin.ConnectionName.Name):
                        base.RaisePropertyChanged(nameof(ConnectionName));
                        break;
                }
            };
        }
    }
}