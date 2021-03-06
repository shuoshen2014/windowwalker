﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowWalker.Components
{
    /// <summary>
    /// Responsible for searching and finding matches for the strings provided.
    /// Essentially the UI independent model of the application
    /// </summary>
    class WindowSearchController
    {
        #region Members

        /// <summary>
        /// the current search text
        /// </summary>
        private string searchText;

        /// <summary>
        /// Open window search results
        /// </summary
        private List<Window> searchMatches;

        /// <summary>
        /// Singleton pattern
        /// </summary>
        private static WindowSearchController instance;

        #endregion

        #region Events

        /// <summary>
        /// Delegate handler for open windows updates
        /// </summary>
        public delegate void SearchResultUpdateHandler(object sender, Window.WindowListUpdateEventArgs e);

        /// <summary>
        /// Event raised when there is an update to the list of open windows
        /// </summary>
        public event SearchResultUpdateHandler OnSearchResultUpdate;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current search text
        /// </summary>
        public string SearchText
        {
            get { return searchText; }
            set 
            { 
                searchText = value.ToLower();
                this.SearchTextUpdated();
            }
        }

        /// <summary>
        /// Gets the open window search results
        /// </summary>
        public List<Window> SearchMatches
        {
            get 
            { return new List<Window>(searchMatches); }
        }

        /// <summary>
        /// Singleton Pattern
        /// </summary>
        public static WindowSearchController Instance
        {
            get
            {
                if (WindowSearchController.instance == null)
                {
                    WindowSearchController.instance = new WindowSearchController();
                }

                return WindowSearchController.instance;
            }

        }

        #endregion

        /// <summary>
        /// Initializes the search controller object
        /// </summary>
        private WindowSearchController()
        {
            this.searchText = string.Empty;
            OpenWindows.Instance.OnOpenWindowsUpdate += OpenWindowsUpdateHandler;
        }

        /// <summary>
        /// Event handler for when the search text has been updated
        /// </summary>
        public void SearchTextUpdated()
        {
            OpenWindows.Instance.UpdateOpenWindowsList();

            this.SyncOpenWindowsWithModelAsync();
        }

        #region Event Handlers

        public void OpenWindowsUpdateHandler(object sender, Window.WindowListUpdateEventArgs e)
        {
            this.SyncOpenWindowsWithModelAsync();
        }

        /// <summary>
        /// Syncs the open windows with the OpenWindows Model
        /// </summary>
        private async void SyncOpenWindowsWithModelAsync()
        {
            System.Diagnostics.Debug.Print("Syncing WindowSearch result with OpenWindows Model");

            List<Window> snapshotOfOpenWindows = OpenWindows.Instance.Windows;

            if (this.SearchText == string.Empty)
            {
                this.searchMatches = 
                    (snapshotOfOpenWindows.Where(x => !string.IsNullOrEmpty(x.Title)).ToList());
            }
            else
            {
                this.searchMatches = await FuzzySearchOpenWindows(snapshotOfOpenWindows);                    
            }

            if (this.OnSearchResultUpdate != null)
            {
                this.OnSearchResultUpdate(this, new Window.WindowListUpdateEventArgs());
            }
        }

        private Task<List<Window>> FuzzySearchOpenWindows(List<Window> openWindows)
        {
            return Task.Run(
                () =>
                    openWindows.Where(
                     x => (WindowSearchController.IsFuzzyMatch(this.searchText.ToLower(), x.Title.ToLower()) || WindowSearchController.IsFuzzyMatch(this.searchText.ToLower(), x.ProcessName.ToLower())) &&
                         x.Title.Length != 0
                     ).ToList<Window>()
                );
        }

        #endregion

        #region Static Methods

        private static bool IsFuzzyMatch(string searchText, string text)
        {
            int searchStartIndex = 0;
            int letterIndex;
            int lettersInBetween = 0;

            foreach (char letter in searchText)
            {
                if (searchStartIndex >= text.Length)
                {
                    return false;
                }
                else
                {
                    letterIndex = text.IndexOf(letter, searchStartIndex);

                    if (letterIndex != -1)
                    {
                        searchStartIndex = letterIndex + 1;
                    }
                    else
                    {
                        return false;
                    }
                }

                lettersInBetween++;
            }

            return true;
        }

        #endregion
    }
}
