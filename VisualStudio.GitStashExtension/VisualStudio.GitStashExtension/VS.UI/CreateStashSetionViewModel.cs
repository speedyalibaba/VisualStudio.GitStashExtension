﻿using Microsoft.TeamFoundation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VisualStudio.GitStashExtension.Annotations;
using VisualStudio.GitStashExtension.GitHelpers;

namespace VisualStudio.GitStashExtension.VS.UI
{
    public class CreateStashSetionViewModel : INotifyPropertyChanged
    {
        private readonly ITeamExplorer _teamExplorer;
        private readonly IServiceProvider _serviceProvider;
        private readonly GitCommandExecuter _gitCommandExecuter;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public CreateStashSetionViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _teamExplorer = _serviceProvider.GetService(typeof(ITeamExplorer)) as ITeamExplorer;
            _gitCommandExecuter = new GitCommandExecuter(serviceProvider);
        }

        public void CreateStash(bool onlyStaged)
        {
            string errorMessage = string.Empty;
            if ((!onlyStaged && _gitCommandExecuter.TryCreateStash(_message, out errorMessage)) || (onlyStaged && _gitCommandExecuter.TryCreateStashStaged(_message, out errorMessage)))
            {
                _teamExplorer.CurrentPage.RefreshPageAndSections();
            }
            else
            {
                _teamExplorer?.ShowNotification(errorMessage, NotificationType.Error, NotificationFlags.None, null, Guid.NewGuid());
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
