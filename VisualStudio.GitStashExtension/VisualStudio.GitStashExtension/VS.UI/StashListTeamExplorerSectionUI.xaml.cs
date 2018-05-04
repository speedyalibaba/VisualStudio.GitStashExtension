﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls;
using VisualStudio.GitStashExtension.GitHelpers;
using VisualStudio.GitStashExtension.Models;

namespace VisualStudio.GitStashExtension.VS.UI
{
    /// <summary>
    /// Interaction logic for StashTeamExplorerSectionUI.xaml
    /// </summary>
    public partial class StashListTeamExplorerSectionUI : UserControl
    {
        private readonly StashListSectionViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITeamExplorer _teamExplorer;

        public StashListTeamExplorerSectionUI(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _teamExplorer = _serviceProvider.GetService(typeof(ITeamExplorer)) as ITeamExplorer;
            DataContext = _viewModel = new StashListSectionViewModel(serviceProvider);
        }

        public void Refresh()
        {
            SearchText.Text = string.Empty;
            _viewModel.UpdateStashList(string.Empty);
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.UpdateStashList(SearchText.Text);
        }

        private void StashInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var stash = menuItem.Tag as Stash;
            var stashInfo = _viewModel.GetStashInfo(stash.Id);

            if (stashInfo != null)
            {
                stash.ChangedFiles = stashInfo.ChangedFiles;
                _teamExplorer.NavigateToPage(new Guid(Constants.StashInfoPageId), stash);
            }
        }

        private void ApplyStashMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var stashId = menuItem.Tag as int?;

            var doalogResult = MessageBox.Show("SourceControl - Git" + Environment.NewLine + Environment.NewLine + "Are you sure you want to apply the stash " + menuItem.DisplayMemberPath + "?", "Microsoft Visual Studio", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (doalogResult == MessageBoxResult.Yes)
            {
                if (stashId.HasValue)
                {
                    _viewModel.ApplyStash(stashId.Value);
                }
            }
        }

        private void DeleteStashMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var stashId = menuItem.Tag as int?;

            var doalogResult = MessageBox.Show("SourceControl - Git" + Environment.NewLine + Environment.NewLine + "Are you sure you want to delete the stash " + menuItem.DisplayMemberPath + "?", "Microsoft Visual Studio", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (doalogResult == MessageBoxResult.Yes)
            {
                if (stashId.HasValue)
                {
                    _viewModel.DeleteStash(stashId.Value);
                }
            }
        }

        private void ListItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var listItem = sender as TextBlock;
                var stash = listItem.Tag as Stash;

                var stashInfo = _viewModel.GetStashInfo(stash.Id);

                if (stashInfo != null)
                {
                    stash.ChangedFiles = stashInfo.ChangedFiles;
                    _teamExplorer.NavigateToPage(new Guid(Constants.StashInfoPageId), stash);
                }
            }
        }

        private void PreviewMouseWheelForListView(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
                return;

            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            };
            var parent = ((Control)sender).Parent as UIElement;
            parent?.RaiseEvent(eventArg);
        }
    }
}
