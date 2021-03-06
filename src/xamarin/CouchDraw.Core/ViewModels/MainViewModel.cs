﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CouchDraw.Core.Repositories;
using CouchDraw.Models;
using Robo.Mvvm;
using Robo.Mvvm.ViewModels;
using System.Windows.Input;
using Robo.Mvvm.Input;

namespace CouchDraw.Core.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        Action UpdateCanvas { get; set; }
        string SelectedPathColor { get; set; } = "#000000";

        public List<Path> Paths { get; set; } = new List<Path>();
        public List<Path> ExternalPaths = new List<Path>();


        ICommand _selectPathColorCommand;
        public ICommand SelectPathColorCommand
        {
            get
            {
                if (_selectPathColorCommand == null)
                {
                    _selectPathColorCommand = new Command<string>((color) => SelectedPathColor = color);
                }

                return _selectPathColorCommand;
            }
        }

        ICommand _undoPathCommand;
        public ICommand UndoPathCommand
        {
            get
            {
                if (_undoPathCommand == null)
                {
                    _undoPathCommand = new Command(UndoLastPath);
                }

                return _undoPathCommand;
            }
        }

        ICommand _clearAllPathsCommand;
        public ICommand ClearAllPathsCommand
        {
            get
            {
                if (_clearAllPathsCommand == null)
                {
                    _clearAllPathsCommand = new Command(ClearAllPaths);
                }

                return _clearAllPathsCommand;
            }
        }

        ICommand _blankCanvas;
        public ICommand BlankCanvas
        {
            get
            {
                if (_blankCanvas == null)
                {
                    _blankCanvas = new Command(ClearAll);
                }

                return _blankCanvas;
            }
        }


        ICommand _forceUpdate;
        public ICommand ForceUpdateCommand
        {
            get
            {
                if (_forceUpdate == null)
                {
                    _forceUpdate = new Command(forceUpdate);
                }

                return _forceUpdate;
            }
        }
        

        ICanvasRepository CanvasRepository { get; set; }

        public MainViewModel(Action updateCanvas)
        {
            UpdateCanvas = updateCanvas;

            CanvasRepository = ServiceContainer.GetInstance<ICanvasRepository>();

            Init();
        }

        // We want to kick off this method, we don't need to (a)wait for the response.
        private async void Init()
        {
            var internalPaths = await CanvasRepository.GetInternalPathsAsync().ConfigureAwait(false);
            var externalPaths = await Task.Run(() => CanvasRepository.GetExternalPaths(UpdatePaths));

            if (internalPaths?.Count > 0)
            {
                Paths = internalPaths;
            } else
            {
                Paths = new List<Path>();
            }

            if (externalPaths?.Count > 0)
            {
                ExternalPaths = externalPaths;
            } else
            {
                ExternalPaths = new List<Path>();
            }

            UpdateCanvas?.Invoke();

        }

        public void CreatePath(Point point)
        {
            var path = new Path(Guid.NewGuid().ToString())
            {
                CreatedBy = AppInstance.AppId,
                Color = SelectedPathColor
            };

            path.Points.Add(point);

            Paths.Add(path);

            SavePath(path);
        }
        
        public void AddPoint(Point point)
        {

            
            if (Paths != null)
            {
                var path = Paths.Last();

                if (path != null)
                {
                    path.Points?.Add(point);
                    SavePath(path);
                }
            }
        }

        void SavePath(Path path)
        {
            Task.Run(() => CanvasRepository?.SavePath(path));
            UpdateCanvas?.Invoke();
        }

        void UpdatePaths(List<Path> paths)
        {
            ExternalPaths = paths;
            var internalPaths = CanvasRepository.GetInternalPaths();

            

            if (internalPaths == null || internalPaths?.Count == 0)
            {
                Paths = new List<Path>();
            }


            UpdateCanvas?.Invoke();
        }

        void UndoLastPath()
        {
            var path = Paths?.Last();

            if (path != null)
            {
                CanvasRepository?.DeletePath(path);
                Paths.Remove(path);
                UpdateCanvas?.Invoke();
            }
        }

        void ClearAllPaths()
        {
            CanvasRepository?.DeletePaths(Paths);
            Paths = new List<Path>();
            UpdateCanvas?.Invoke();
        }


        void ClearAll()
        {
            

            CanvasRepository?.DeleteAllPaths();
            Paths = new List<Path>();
            UpdateCanvas?.Invoke();

            Paths = new List<Path>();
            ExternalPaths = new List<Path>();
        }

        void forceUpdate()
        {
            Init();
            CheckInternetConnection();
        }


        public bool CheckInternetConnection()
        {
            string CheckUrl = "http://google.com";

            try
            {
                System.Net.HttpWebRequest iNetRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(CheckUrl);

                iNetRequest.Timeout = 3000;

                System.Net.WebResponse iNetResponse = iNetRequest.GetResponse();

                Acr.UserDialogs.UserDialogs.Instance.Toast("Internet is up! ");
                iNetResponse.Close();

                return true;

            }
            catch (System.Net.WebException ex)
            {
                Acr.UserDialogs.UserDialogs.Instance.Toast("Oh... internet is down! but your changes were saved locally.");

                return false;
            }
        }
    }
}
